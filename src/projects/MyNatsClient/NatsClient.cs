using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
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
        private const int MaxReconnectDueToFailureAttempts = 5;

        private readonly object _sync;
        private readonly ConnectionInfo _connectionInfo;
        private readonly ConcurrentDictionary<string, ClientSubscription> _subscriptions;
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
            _subscriptions = new ConcurrentDictionary<string, ClientSubscription>();
            _publisher = new Publisher(DoSend, DoSendAsync, DoSend, DoSendAsync);
            _eventMediator = new ObservableOf<IClientEvent>();
            _opMediator = new NatsOpMediator();

            _socketIsConnected = () => _socket != null && _socket.Connected;
            _consumerIsCancelled = () => _cancellation == null || _cancellation.IsCancellationRequested;

            Id = id;
            State = NatsClientState.Disconnected;
            SocketFactory = new SocketFactory();

            SubscribeToClientEventsForInternalUse();
        }

        private void SubscribeToClientEventsForInternalUse()
        {
            Events.Subscribe(new DelegatingObserver<IClientEvent>(ev =>
            {
                if (ev is ClientConnected)
                {
                    OnClientConnected();
                    return;
                }

                var disconnectedEvent = ev as ClientDisconnected;
                if (disconnectedEvent != null)
                {
                    OnClientDisconnected(disconnectedEvent);
                    // ReSharper disable once RedundantJumpStatement
                    return;
                }
            }));
        }

        private void OnClientConnected()
        {
            foreach (var subscription in _subscriptions.Values)
            {
                if (State != NatsClientState.Connected)
                    break;

                DoSub(subscription.SubscriptionInfo);
            }
        }

        private void OnClientDisconnected(ClientDisconnected disconnectedEvent)
        {
            if (disconnectedEvent == null || disconnectedEvent.Reason != DisconnectReason.DueToFailure)
                return;

            try
            {
                var attempts = 0;
                while (State == NatsClientState.Disconnected && attempts < MaxReconnectDueToFailureAttempts)
                {
                    attempts += 1;
                    Logger.Debug($"Trying to reconnect after disconnection due to failure. attempt={attempts.ToString()}");
                    Connect();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed while trying to reconnect client.", ex);
            }

            if (State == NatsClientState.Disconnected)
                _eventMediator.Dispatch(new ClientAutoReconnectFailed(this));
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            Dispose(true);
            GC.SuppressFinalize(this);
            _isDisposed = true;
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed || !disposing)
                return;

            Release();

            var subscriptions = _subscriptions.Values.Cast<IDisposable>().ToArray();
            Try.DisposeAll(subscriptions);
            _subscriptions.Clear();

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
                throw new InvalidOperationException("Can not send. Client is not connected.");
        }

        public void Disconnect()
        {
            ThrowIfDisposed();

            DoDisconnect(DisconnectReason.ByUser);
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
                        if (DoConnectTo(hosts.Dequeue()))
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
        private bool DoConnectTo(Host host)
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

        //public MsgOp RequestAsync(string subject, string body)
        //{
        //    ThrowIfDisposed();
        //}

        //public MsgOp RequestAsync(string subject, byte[] body)
        //{
        //    ThrowIfDisposed();
        //}

        //public MsgOp RequestAsync(string subject, IPayload body)
        //{
        //    ThrowIfDisposed();
        //}

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

        public IClientSubscription Sub(string subject)
            => Sub(new SubscriptionInfo(subject));

        public IClientSubscription Sub(SubscriptionInfo subscriptionInfo)
            => Sub(subscriptionInfo, msgs => Disposable.Empty);

        public IClientSubscription Sub(string subject, Action<MsgOp> handler)
            => Sub(new SubscriptionInfo(subject), new DelegatingObserver<MsgOp>(handler));

        public IClientSubscription Sub(string subject, IObserver<MsgOp> observer)
            => Sub(new SubscriptionInfo(subject), observer);

        public IClientSubscription Sub(string subject, Func<IFilterableObservable<MsgOp>, IDisposable> subscriptionFactory)
            => Sub(new SubscriptionInfo(subject), subscriptionFactory);

        public IClientSubscription Sub(SubscriptionInfo subscriptionInfo, Action<MsgOp> handler)
            => Sub(subscriptionInfo, new DelegatingObserver<MsgOp>(handler));

        public IClientSubscription Sub(SubscriptionInfo subscriptionInfo, IObserver<MsgOp> observer)
            => Sub(subscriptionInfo, msgStream => msgStream.Subscribe(observer, msg => subscriptionInfo.Matches(msg.Subject)));

        public IClientSubscription Sub(SubscriptionInfo subscriptionInfo, Func<IFilterableObservable<MsgOp>, IDisposable> subscriptionFactory)
        {
            ThrowIfDisposed();

            var subscription = CreateMsgOpSubscription(subscriptionInfo, subscriptionFactory);

            if (State != NatsClientState.Connected)
                return subscription;

            DoSub(subscriptionInfo);

            return subscription;
        }

        private void DoSub(SubscriptionInfo subscriptionInfo) => WithWriteLock(() =>
        {
            DoSend(SubCmd.Generate(subscriptionInfo.Subject, subscriptionInfo.Id, subscriptionInfo.QueueGroup));
            if (subscriptionInfo.MaxMessages.HasValue)
                DoSend(UnSubCmd.Generate(subscriptionInfo.Id, subscriptionInfo.MaxMessages));
            DoFlush();
        });

        public Task<IClientSubscription> SubAsync(string subject)
            => SubAsync(new SubscriptionInfo(subject));

        public Task<IClientSubscription> SubAsync(string subject, Action<MsgOp> handler)
            => SubAsync(new SubscriptionInfo(subject), new DelegatingObserver<MsgOp>(handler));

        public Task<IClientSubscription> SubAsync(string subject, IObserver<MsgOp> observer)
            => SubAsync(new SubscriptionInfo(subject), observer);

        public Task<IClientSubscription> SubAsync(string subject, Func<IFilterableObservable<MsgOp>, IDisposable> subscriptionFactory)
            => SubAsync(new SubscriptionInfo(subject), subscriptionFactory);

        public Task<IClientSubscription> SubAsync(SubscriptionInfo subscriptionInfo)
            => SubAsync(subscriptionInfo, msgs => Disposable.Empty);

        public Task<IClientSubscription> SubAsync(SubscriptionInfo subscriptionInfo, Action<MsgOp> handler)
            => SubAsync(subscriptionInfo, new DelegatingObserver<MsgOp>(handler));

        public Task<IClientSubscription> SubAsync(SubscriptionInfo subscriptionInfo, IObserver<MsgOp> observer)
            => SubAsync(subscriptionInfo, msgStream => msgStream.Subscribe(observer, msg => subscriptionInfo.Matches(msg.Subject)));

        public async Task<IClientSubscription> SubAsync(SubscriptionInfo subscriptionInfo, Func<IFilterableObservable<MsgOp>, IDisposable> subscriptionFactory)
        {
            ThrowIfDisposed();

            var subscription = CreateMsgOpSubscription(subscriptionInfo, subscriptionFactory);

            if (State != NatsClientState.Connected)
                return subscription;

            await DoSubAsync(subscriptionInfo).ForAwait();

            return subscription;
        }

        private async Task DoSubAsync(SubscriptionInfo subscriptionInfo) => await WithWriteLockAsync(async () =>
        {
            await DoSendAsync(SubCmd.Generate(subscriptionInfo.Subject, subscriptionInfo.Id, subscriptionInfo.QueueGroup))
                .ForAwait();
            if (subscriptionInfo.MaxMessages.HasValue)
                await DoSendAsync(UnSubCmd.Generate(subscriptionInfo.Id, subscriptionInfo.MaxMessages)).ForAwait();
            await DoFlushAsync().ForAwait();
        }).ForAwait();

        private ClientSubscription CreateMsgOpSubscription(SubscriptionInfo subscriptionInfo, Func<IFilterableObservable<MsgOp>, IDisposable> subscriptionFactory)
        {
            var subscription = ClientSubscription.Create(subscriptionInfo, subscriptionFactory(MsgOpStream), info =>
            {
                if (!TryRemoveSubscription(info))
                    return;

                Swallow.Everything(() => Unsub(info));
            });

            if (!_subscriptions.TryAdd(subscription.SubscriptionInfo.Id, subscription))
                throw new NatsException($"Could not create subscription. Id='{subscriptionInfo.Id}'. Subject='{subscriptionInfo.Subject}' QueueGroup='{subscriptionInfo.QueueGroup}'.");

            return subscription;
        }

        public void Unsub(SubscriptionInfo subscriptionInfo)
        {
            EnsureArg.IsNotNull(subscriptionInfo, nameof(subscriptionInfo));

            ThrowIfDisposed();

            if (!TryRemoveSubscription(subscriptionInfo))
                return;

            if (State != NatsClientState.Connected)
                return;

            WithWriteLock(() =>
            {
                if (State != NatsClientState.Connected)
                    return;

                DoSend(UnSubCmd.Generate(subscriptionInfo.Id, subscriptionInfo.MaxMessages));
                DoFlush();
            });
        }

        public async Task UnsubAsync(SubscriptionInfo subscriptionInfo)
        {
            EnsureArg.IsNotNull(subscriptionInfo, nameof(subscriptionInfo));

            ThrowIfDisposed();

            if (!TryRemoveSubscription(subscriptionInfo))
                return;

            if (State != NatsClientState.Connected)
                return;

            await WithWriteLockAsync(async () =>
            {
                if (State != NatsClientState.Connected)
                    return;

                await DoSendAsync(UnSubCmd.Generate(subscriptionInfo.Id, subscriptionInfo.MaxMessages)).ForAwait();
                await DoFlushAsync().ForAwait();
            }).ForAwait();
        }

        private bool TryRemoveSubscription(SubscriptionInfo subscriptionInfo)
        {
            ClientSubscription tmp;

            _subscriptions.TryRemove(subscriptionInfo.Id, out tmp);

            return tmp != null;
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