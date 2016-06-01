using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
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

        private const int ConsumerMaxSpinWaitMs = 500;
        private const int ConsumerIfNoDataWaitForMs = 100;
        private const int TryConnectMaxCycleDelayMs = 200;
        private const int TryConnectMaxDurationMs = 2000;

        private readonly object _sync;
        private readonly ConnectionInfo _connectionInfo;
        private readonly Func<bool> _socketIsConnected;
        private readonly Func<bool> _consumerIsCancelled;
        private readonly Func<bool> _hasData;
        private ObservableOf<IClientEvent> _eventMediator;
        private NatsOpMediator _opMediator;
        private Socket _socket;
        private NetworkStream _readStream;
        private NetworkStream _writeStream;
        private SemaphoreSlim _writeStreamSync;
        private NatsOpStreamReader _reader;
        private Task _consumer;
        private CancellationTokenSource _cancellation;
        private NatsServerInfo _serverInfo;
        private bool _isDisposed;

        public string Id { get; }
        public IObservable<IClientEvent> Events => _eventMediator;
        public IObservable<IOp> IncomingOps => _opMediator;
        public INatsClientStats Stats => _opMediator;
        public NatsClientState State { get; private set; }
        public ISocketFactory SocketFactory { private get; set; }

        public NatsClient(string id, ConnectionInfo connectionInfo)
        {
            _sync = new object();
            _writeStreamSync = new SemaphoreSlim(1, 1);
            _connectionInfo = connectionInfo.Clone();
            _eventMediator = new ObservableOf<IClientEvent>();
            _opMediator = new NatsOpMediator();

            _socketIsConnected = () => _socket != null && _socket.Connected;
            _consumerIsCancelled = () => _cancellation == null || _cancellation.IsCancellationRequested;
            _hasData = () =>
                    _socketIsConnected() &&
                    _readStream != null &&
                    _readStream.CanRead &&
                    _readStream.DataAvailable;
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

            _writeStreamSync?.Dispose();
            _writeStreamSync = null;

            _eventMediator?.Dispose();
            _eventMediator = null;

            _opMediator?.Dispose();
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

        private void OnFailed(Exception ex)
            => _eventMediator.Dispatch(new ClientFailed(this, ex));

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
                    //TODO: Potentially track ping times and/or use statistical endpoints of each node to pick best suited
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

                    throw NatsException.NoConnectionCouldBeMade(ex);
                }
            }

            OnConnected();
        }

        //TODO: SSL
        //TODO: Use async connect
        private bool ConnectTo(Host host)
        {
            _socket = _socket ?? SocketFactory.Create();
            _socket.Connect(host.Address, host.Port);
            _writeStream = new NetworkStream(_socket, FileAccess.Write, false);
            _readStream = new NetworkStream(_socket, FileAccess.Read, false);
            _reader = new NatsOpStreamReader(_readStream, _hasData);

            var op = Retry.This(() => _reader.ReadOp().FirstOrDefault(), TryConnectMaxCycleDelayMs, TryConnectMaxDurationMs);
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

            _serverInfo = NatsServerInfo.Parse(infoOp);

            if (_serverInfo.AuthRequired && _connectionInfo.Credentials == Credentials.Empty)
                throw new NatsException($"Error while connecting to {host}. Server requires credentials to be passed. None was specified.");

            _opMediator.Dispatch(infoOp);

            _socket.Send(NatsEncoder.GetBytes(GenerateConnectionOpString()));

            if (!_socket.Connected)
            {
                Logger.Error($"Error while connecting to {host}. No connection could be established.");
                return false;
            }

            _cancellation = new CancellationTokenSource();
            _consumer = Task.Factory.StartNew(
                Consumer,
                _cancellation.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).ContinueWith(OnConsumerCompleted);

            return true;
        }

        private string GenerateConnectionOpString()
        {
            var sb = new StringBuilder();
            sb.Append("CONNECT {\"name\":\"mynatsclient\",\"lang\":\"csharp\",\"verbose\":");
            sb.Append(_connectionInfo.Verbose.ToString().ToLower());

            if (_connectionInfo.Credentials != Credentials.Empty)
            {
                sb.Append(",\"user\":\"");
                sb.Append(_connectionInfo.Credentials.User);
                sb.Append("\",\"pass\":\"");
                sb.Append(_connectionInfo.Credentials.Pass);
                sb.Append("\"");
            }
            sb.Append("}");
            sb.Append(NatsEncoder.Crlf);

            return sb.ToString();
        }

        private ErrOp Consumer()
        {
            ErrOp errOp = null;

            var noDataCount = 0;

            while (_socketIsConnected() && !_consumerIsCancelled() && errOp == null)
            {
                SpinWait.SpinUntil(() => !_socketIsConnected() || _consumerIsCancelled() || _hasData(), ConsumerMaxSpinWaitMs);
                if (!_socketIsConnected())
                    break;

                if (_consumerIsCancelled())
                    break;

                if (!_hasData())
                {
                    noDataCount += 1;

                    //TODO: Use Stats.LastOpReceivedAt to see if we should force disconnect
                    if (noDataCount >= 5)
                    {
                        Ping();
                        continue;
                    }

                    Thread.Sleep(ConsumerIfNoDataWaitForMs);
                    continue;
                }

                noDataCount = 0;

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

                Logger.Fatal("Consumer exception.", ex);
                OnFailed(ex);
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
                        _writeStream?.Close();
                        _writeStream?.Dispose();
                        _writeStream = null;
                    },
                    () =>
                    {
                        if (_consumer == null || !_consumer.IsCompleted)
                            return;

                        _consumer.Dispose();
                        _consumer = null;
                    },
                    () =>
                    {
                        _reader?.Dispose();
                        _reader = null;
                    },
                    () =>
                    {
                        _readStream?.Close();
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
                            _socket?.Close();
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

            DoSend(PingCmd.Generate());
        }

        public async Task PingAsync()
        {
            ThrowIfDisposed();

            await DoSendAsync(PingCmd.Generate()).ForAwait();
        }

        public void Pong()
        {
            ThrowIfDisposed();

            DoSend(PongCmd.Generate());
        }

        public async Task PongAsync()
        {
            ThrowIfDisposed();

            await DoSendAsync(PongCmd.Generate()).ForAwait();
        }

        public void Pub(string subject, string body, string replyTo = null)
        {
            ThrowIfDisposed();

            DoSend(PubCmd.Generate(subject, body, replyTo));
        }

        public void Pub(string subject, byte[] body, string replyTo = null)
        {
            ThrowIfDisposed();

            DoSend(PubCmd.Generate(subject, body, replyTo));
        }

        public async Task PubAsync(string subject, byte[] body, string replyTo = null)
        {
            ThrowIfDisposed();

            await DoSendAsync(PubCmd.Generate(subject, body, replyTo)).ForAwait();
        }

        public async Task PubAsync(string subject, string body, string replyTo = null)
        {
            ThrowIfDisposed();

            await DoSendAsync(PubCmd.Generate(subject, body, replyTo)).ForAwait();
        }

        public void Sub(string subject, string subscriptionId, string queueGroup = null)
        {
            ThrowIfDisposed();

            DoSend(SubCmd.Generate(subject, subscriptionId, queueGroup));
        }

        public async Task SubAsync(string subject, string subscriptionId, string queueGroup = null)
        {
            ThrowIfDisposed();

            await DoSendAsync(SubCmd.Generate(subject, subscriptionId, queueGroup)).ForAwait();
        }

        public void UnSub(string subscriptionId, int? maxMessages = null)
        {
            ThrowIfDisposed();

            DoSend(UnSubCmd.Generate(subscriptionId, maxMessages));
        }

        public async Task UnSubAsync(string subscriptionId, int? maxMessages = null)
        {
            ThrowIfDisposed();

            await DoSendAsync(UnSubCmd.Generate(subscriptionId, maxMessages)).ForAwait();
        }

        private void DoSend(byte[] data)
        {
            ThrowIfNotConnected();

            if (data.Length > _serverInfo.MaxPayload)
                throw NatsException.ExceededMaxPayload(_serverInfo.MaxPayload, data.Length);

            try
            {
                _writeStreamSync.Wait(_cancellation.Token);
                _writeStream.Write(data, 0, data.Length);
                _writeStream.Flush();
            }
            finally
            {
                _writeStreamSync.Release();
            }
        }

        private async Task DoSendAsync(byte[] data)
        {
            ThrowIfNotConnected();

            if (data.Length > _serverInfo.MaxPayload)
                throw NatsException.ExceededMaxPayload(_serverInfo.MaxPayload, data.Length);

            try
            {
                await _writeStreamSync.WaitAsync(_cancellation.Token).ForAwait();
                await _writeStream.WriteAsync(data, 0, data.Length, _cancellation.Token).ForAwait();
                await _writeStream.FlushAsync(_cancellation.Token).ForAwait();
            }
            finally
            {
                _writeStreamSync.Release();
            }
        }
    }
}