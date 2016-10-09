using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MyNatsClient.Events;
using MyNatsClient.Internals;
using MyNatsClient.Internals.Commands;
using MyNatsClient.Internals.Extensions;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public class NatsClient : INatsClient, IDisposable
    {
        private static readonly ILogger Logger = LoggerManager.Resolve(typeof(NatsClient));

        private const int ConsumerPingAfterMsSilenceFromServer = 30000;
        private const int ConsumerMaxMsSilenceFromServer = 60000;
        private const int TryConnectMaxCycleDelayMs = 200;
        private const int TryConnectMaxDurationMs = 2000;

        private readonly object _sync;
        private readonly ConnectionInfo _connectionInfo;
        private readonly Func<bool> _socketIsConnected;
        private readonly Func<bool> _consumerIsCancelled;
        private readonly IPublisher _publisher;
        private ObservableOf<IClientEvent> _eventMediator;
        private NatsOpMediator _opMediator;
        private Socket _socket;
        private Stream _readStream;
        private Stream _writeStream;
        private Locker _writeStreamSync;
        private NatsOpStreamReader _reader;
        private Task _consumer;
        private CancellationTokenSource _cancellation;
        private NatsServerInfo _serverInfo;
        private bool _isDisposed;

        private bool ShouldAutoFlush => _connectionInfo.PubFlushMode != PubFlushMode.Manual;

        public string Id { get; }
        public IObservable<IClientEvent> Events => _eventMediator;
        public IObservable<IOp> OpStream => _opMediator;
        public IFilterableObservable<MsgOp> MsgOpStream => _opMediator;
        public INatsClientStats Stats => _opMediator;
        public NatsClientState State { get; private set; }
        public ISocketFactory SocketFactory { private get; set; }

        public NatsClient(string id, ConnectionInfo connectionInfo)
        {
            _sync = new object();
            _writeStreamSync = new Locker();
            _connectionInfo = connectionInfo.Clone();
            _publisher = new Publisher(DoSend, DoSendAsync, DoSend, DoSendAsync);
            _eventMediator = new ObservableOf<IClientEvent>();
            _opMediator = new NatsOpMediator();

            _socketIsConnected = () => _socket != null && _socket.Connected;
            _consumerIsCancelled = () => _cancellation == null || _cancellation.IsCancellationRequested;

            Id = id;
            State = NatsClientState.Disconnected;
            SocketFactory = new SocketFactory();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            _isDisposed = true;
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed || !disposing)
                return;

            Release();

            Try.DisposeAll(_writeStreamSync, _eventMediator, _opMediator);
            _writeStreamSync = null;
            _eventMediator = null;
            _opMediator = null;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        private void ThrowIfNotConnected()
        {
            if (State != NatsClientState.Connected)
                throw new InvalidOperationException($"Can not send. Client is not {NatsClientState.Connected}");
        }

        public void Disconnect()
        {
            ThrowIfDisposed();

            DoDisconnect(DisconnectReason.ByConsumer);
        }

        private void DoDisconnect(DisconnectReason reason)
        {
            if (State != NatsClientState.Connected)
                return;

            lock (_sync)
            {
                if (State != NatsClientState.Connected)
                    return;

                Release();

                State = NatsClientState.Disconnected;
            }

            OnDisconnected(reason);
        }

        private void OnConnected()
            => _eventMediator.Dispatch(new ClientConnected(this));

        private void OnDisconnected(DisconnectReason reason)
            => _eventMediator.Dispatch(new ClientDisconnected(this, reason));

        private void OnConsumerFailed(Exception ex)
            => _eventMediator.Dispatch(new ClientConsumerFailed(this, ex));

        public void Connect()
        {
            ThrowIfDisposed();

            if (State == NatsClientState.Connected || State == NatsClientState.Connecting)
                return;

            lock (_sync)
            {
                if (State == NatsClientState.Connected || State == NatsClientState.Connecting)
                    return;

                State = NatsClientState.Connecting;
                Release();

                try
                {
                    var hosts = new Queue<Host>(_connectionInfo.Hosts.GetRandomized());
                    while (hosts.Any())
                    {
                        if (ConnectTo(hosts.Dequeue()))
                            break;
                    }

                    if (!_socketIsConnected())
                        throw NatsException.NoConnectionCouldBeMade();

                    State = NatsClientState.Connected;
                }
                catch (Exception ex)
                {
                    Release();
                    State = NatsClientState.Disconnected;

                    Logger.Error("Exception while connecting.", ex);

                    throw NatsException.NoConnectionCouldBeMade(ex);
                }
            }

            OnConnected();
        }

        //TODO: SSL
        private bool ConnectTo(Host host)
        {
            _socket = _socket ?? SocketFactory.Create(_connectionInfo.SocketOptions);
            _socket.Connect(host.Address, host.Port);
            _writeStream = new BufferedStream(_socket.CreateWriteStream(), _socket.SendBufferSize);
            _readStream = new BufferedStream(_socket.CreateReadStream(), _socket.ReceiveBufferSize);
            _reader = new NatsOpStreamReader(_readStream);
            Func<IOp> readOne = () => Retry.This(() => _reader.ReadOp().FirstOrDefault(), TryConnectMaxCycleDelayMs, TryConnectMaxDurationMs);

            var op = readOne();
            if (op == null)
            {
                Logger.Error($"Error while connecting to {host}. Expected to get INFO after connection. Got nothing.");
                return false;
            }

            var infoOp = op as InfoOp;
            if (infoOp == null)
            {
                Logger.Error($"Error while connecting to {host}. Expected to get INFO after connection. Got {op.GetAsString()}.");
                return false;
            }

            _serverInfo = NatsServerInfo.Parse(infoOp.Message);
            var credentials = host.HasNonEmptyCredentials() ? host.Credentials : _connectionInfo.Credentials;
            if (_serverInfo.AuthRequired && (credentials == null || credentials == Credentials.Empty))
                throw new NatsException($"Error while connecting to {host}. Server requires credentials to be passed. None was specified.");

            _opMediator.Dispatch(infoOp);

            _socket.Send(ConnectCmd.Generate(_connectionInfo.Verbose, credentials));

            if (!VerifyConnectedOk(host, readOne, ref op))
                return false;

            _opMediator.Dispatch(op);

            _cancellation = new CancellationTokenSource();
            _consumer = Task.Factory.StartNew(
                Consumer,
                _cancellation.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).ContinueWith(OnConsumerCompleted);

            return true;
        }

        private bool VerifyConnectedOk(Host host, Func<IOp> readOne, ref IOp op)
        {
            if (!_socket.Connected)
            {
                Logger.Error($"Error while connecting to {host}. No connection could be established.");
                return false;
            }

            _socket.Send(PingCmd.Generate());

            op = readOne();
            if (op == null)
            {
                Logger.Error($"Error while connecting to {host}. Expected to get INFO after connection. Got nothing.");
                return false;
            }

            if (op is ErrOp)
            {
                Logger.Error($"Error while connecting to {host}. Expected to get PONG after sending CONNECT and PING. Got {op.GetAsString()}.");
                return false;
            }

            if (_socket.Connected)
                return true;

            Logger.Error($"Error while connecting to {host}. No connection could be established.");
            return false;
        }

        private ErrOp Consumer()
        {
            ErrOp errOp = null;

            Func<bool> shouldRead = () =>
                _socketIsConnected() &&
                !_consumerIsCancelled() &&
                _readStream != null &&
                _readStream.CanRead;

            while (_socketIsConnected() && !_consumerIsCancelled() && errOp == null)
            {
                if (!shouldRead())
                    break;

                try
                {
                    foreach (var op in _reader.ReadOp())
                    {
                        if (_connectionInfo.AutoRespondToPing && op is PingOp)
                            Pong();

                        errOp = op as ErrOp;
                        if (errOp != null)
                            continue;

                        _opMediator.Dispatch(op);
                    }
                }
                catch (IOException ioex)
                {
                    var ex = ioex.InnerException as SocketException;
                    if (ex != null)
                    {
                        if (ex.SocketErrorCode == SocketError.Interrupted && _consumerIsCancelled())
                            break;

                        if (ex.SocketErrorCode != SocketError.TimedOut)
                            throw;
                    }

                    var silenceDeltaMs = DateTime.UtcNow.Subtract(Stats.LastOpReceivedAt).TotalMilliseconds;
                    if (silenceDeltaMs >= ConsumerMaxMsSilenceFromServer)
                        throw;

                    if (silenceDeltaMs >= ConsumerPingAfterMsSilenceFromServer)
                        Ping();
                }
            }

            return errOp;
        }

        private void OnConsumerCompleted(Task<ErrOp> t)
        {
            if (!t.IsFaulted && t.Result == null)
                return;

            DoDisconnect(DisconnectReason.DueToFailure);

            if (t.IsFaulted)
            {
                var ex = t.Exception?.GetBaseException();
                if (ex == null)
                    return;

                Logger.Error("Consumer exception.", ex);
                OnConsumerFailed(ex);
            }
            else
            {
                var errOp = t.Result;
                if (errOp == null)
                    return;

                _opMediator.Dispatch(errOp);
            }
        }

        private void Release()
        {
            lock (_sync)
            {
                Try.All(
                    () =>
                    {
                        _cancellation?.Cancel();
                        _cancellation?.Dispose();
                        _cancellation = null;
                    },
                    () =>
                    {
#if !NETSTANDARD1_6
                        _writeStream?.Close();
#endif
                        _writeStream?.Dispose();
                        _writeStream = null;
                    },
                    () =>
                    {
                        if (_consumer == null || !_consumer.IsCompleted)
                            return;
#if !NETSTANDARD1_6
                        _consumer.Dispose();
#endif
                        _consumer = null;
                    },
                    () =>
                    {
#if !NETSTANDARD1_6
                        _readStream?.Close();
#endif
                        _readStream?.Dispose();
                        _readStream = null;
                    },
                    () =>
                    {
                        if (_socket == null)
                            return;

                        if (_socket.Connected)
                        {
                            _socket?.Shutdown(SocketShutdown.Both);
#if !NETSTANDARD1_6
                            _socket?.Close();
#endif
                        }

                        _socket?.Dispose();
                        _socket = null;
                    });

                _serverInfo = null;
            }
        }

        public void Ping()
        {
            ThrowIfDisposed();

            WithWriteLock(() =>
            {
                DoSend(PingCmd.Generate());
                DoFlush();
            });
        }

        public async Task PingAsync()
        {
            ThrowIfDisposed();

            await WithWriteLockAsync(async () =>
            {
                await DoSendAsync(PingCmd.Generate()).ForAwait();
                await DoFlushAsync().ForAwait();
            }).ForAwait();
        }

        public void Pong()
        {
            ThrowIfDisposed();

            WithWriteLock(() =>
            {
                DoSend(PongCmd.Generate());
                DoFlush();
            });
        }

        public async Task PongAsync()
        {
            ThrowIfDisposed();

            await WithWriteLockAsync(async () =>
            {
                await DoSendAsync(PongCmd.Generate()).ForAwait();
                await DoFlushAsync().ForAwait();
            }).ForAwait();
        }

        public void Pub(string subject, string body, string replyTo = null)
        {
            ThrowIfDisposed();

            WithWriteLock(() =>
            {
                DoSend(PubCmd.Generate(subject, body, replyTo));
                if (ShouldAutoFlush)
                    DoFlush();
            });
        }

        public void Pub(string subject, byte[] body, string replyTo = null)
        {
            ThrowIfDisposed();

            WithWriteLock(() =>
            {
                DoSend(PubCmd.Generate(subject, body, replyTo));
                if (ShouldAutoFlush)
                    DoFlush();
            });
        }

        public void Pub(string subject, IPayload body, string replyTo = null)
        {
            ThrowIfDisposed();

            WithWriteLock(() =>
            {
                DoSend(PubCmd.Generate(subject, body, replyTo));
                if (ShouldAutoFlush)
                    DoFlush();
            });
        }

        public async Task PubAsync(string subject, byte[] body, string replyTo = null)
        {
            ThrowIfDisposed();

            await WithWriteLockAsync(async () =>
            {
                await DoSendAsync(PubCmd.Generate(subject, body, replyTo));
                if (ShouldAutoFlush)
                    await DoFlushAsync().ForAwait();
            }).ForAwait();
        }

        public async Task PubAsync(string subject, IPayload body, string replyTo = null)
        {
            ThrowIfDisposed();

            await WithWriteLockAsync(async () =>
            {
                await DoSendAsync(PubCmd.Generate(subject, body, replyTo));
                if (ShouldAutoFlush)
                    await DoFlushAsync().ForAwait();
            }).ForAwait();
        }

        public async Task PubAsync(string subject, string body, string replyTo = null)
        {
            ThrowIfDisposed();

            await WithWriteLockAsync(async () =>
            {
                await DoSendAsync(PubCmd.Generate(subject, body, replyTo));
                if (ShouldAutoFlush)
                    await DoFlushAsync().ForAwait();
            }).ForAwait();
        }

        public void PubMany(Action<IPublisher> p)
        {
            ThrowIfDisposed();

            WithWriteLock(() =>
            {
                p(_publisher);
                if (ShouldAutoFlush)
                    DoFlush();
            });
        }

        public void Flush()
        {
            ThrowIfDisposed();

            WithWriteLock(DoFlush);
        }

        public async Task FlushAsync()
        {
            ThrowIfDisposed();

            await WithWriteLockAsync(DoFlushAsync).ForAwait();
        }

        private void DoFlush()
        {
            ThrowIfNotConnected();

            _writeStream.Flush();
        }

        private async Task DoFlushAsync()
        {
            ThrowIfNotConnected();

            await _writeStream.FlushAsync();
        }

        public void Sub(string subject, string subscriptionId, string queueGroup = null)
        {
            ThrowIfDisposed();

            WithWriteLock(() =>
            {
                DoSend(SubCmd.Generate(subject, subscriptionId, queueGroup));
                DoFlush();
            });
        }

        public async Task SubAsync(string subject, string subscriptionId, string queueGroup = null)
        {
            ThrowIfDisposed();

            await WithWriteLockAsync(async () =>
            {
                await DoSendAsync(SubCmd.Generate(subject, subscriptionId, queueGroup)).ForAwait();
                await DoFlushAsync().ForAwait();
            }).ForAwait();
        }

        public void UnSub(string subscriptionId, int? maxMessages = null)
        {
            ThrowIfDisposed();

            WithWriteLock(() =>
            {
                DoSend(UnSubCmd.Generate(subscriptionId, maxMessages));
                DoFlush();
            });
        }

        public async Task UnSubAsync(string subscriptionId, int? maxMessages = null)
        {
            ThrowIfDisposed();

            await WithWriteLockAsync(async () =>
            {
                await DoSendAsync(UnSubCmd.Generate(subscriptionId, maxMessages)).ForAwait();
                await DoFlushAsync().ForAwait();
            }).ForAwait();
        }

        public Inbox CreateInbox(string subject, Action<MsgOp> onIncoming, int? unsubAfterNMessages = null)
        {
            ThrowIfDisposed();

            var inbox = new Inbox(subject, _opMediator, new DelegatingObserver<MsgOp>(onIncoming));

            Sub(inbox.Subject, inbox.SubscriptionId);

            return inbox;
        }

        private void DoSend(byte[] data)
        {
            ThrowIfNotConnected();

            if (data.Length > _serverInfo.MaxPayload)
                throw NatsException.ExceededMaxPayload(_serverInfo.MaxPayload, data.Length);

            _writeStream.Write(data, 0, data.Length);
        }

        private void DoSend(IPayload data)
        {
            ThrowIfNotConnected();

            if (data.Size > _serverInfo.MaxPayload)
                throw NatsException.ExceededMaxPayload(_serverInfo.MaxPayload, data.Size);

            for (var i = 0; i < data.BlockCount; i++)
                _writeStream.Write(data[i], 0, data[i].Length);
        }

        private async Task DoSendAsync(byte[] data)
        {
            ThrowIfNotConnected();

            if (data.Length > _serverInfo.MaxPayload)
                throw NatsException.ExceededMaxPayload(_serverInfo.MaxPayload, data.Length);

            await _writeStream.WriteAsync(data, 0, data.Length, _cancellation.Token).ForAwait();
        }

        private async Task DoSendAsync(IPayload data)
        {
            ThrowIfNotConnected();

            if (data.Size > _serverInfo.MaxPayload)
                throw NatsException.ExceededMaxPayload(_serverInfo.MaxPayload, data.Size);

            for (var i = 0; i < data.BlockCount; i++)
                await _writeStream.WriteAsync(data[i], 0, data.Size, _cancellation.Token).ForAwait();
        }

        private void WithWriteLock(Action a)
        {
            using (_writeStreamSync.Lock())
                a();
        }

        private async Task WithWriteLockAsync(Func<Task> a)
        {
            using (await _writeStreamSync.LockAsync(_cancellation.Token).ForAwait())
                await a().ForAwait();
        }
    }
}