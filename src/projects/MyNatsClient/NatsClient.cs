using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MyNatsClient.Events;
using MyNatsClient.Extensions;
using MyNatsClient.Internals;
using MyNatsClient.Internals.Commands;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public class NatsClient : INatsClient, IDisposable
    {
        private readonly ILogger _logger = LoggerManager.Resolve(typeof(NatsClient));

        //TODO: Make available as options
        private const int ConsumerPingAfterMsSilenceFromServer = 20000;
        private const int ConsumerMaxMsSilenceFromServer = 40000;
        private const int MaxReconnectDueToFailureAttempts = 5;
        private const int WaitForConsumerCompleteMs = 100;

        private readonly ConnectionInfo _connectionInfo;
        private readonly ConcurrentDictionary<string, Subscription> _subscriptions;
        private readonly INatsConnectionManager _connectionManager;

        private Locker _sync;
        private CancellationTokenSource _cancellation;
        private INatsConnection _connection;
        private Task _consumer;
        private NatsObservableOf<IClientEvent> _eventMediator;
        private NatsOpMediator _opMediator;
        private bool _isDisposed;

        private bool ShouldAutoFlush => _connectionInfo.PubFlushMode != PubFlushMode.Manual;

        public INatsObservable<IClientEvent> Events => _eventMediator;
        public INatsObservable<IOp> OpStream => _opMediator.AllOpsStream;
        public INatsObservable<MsgOp> MsgOpStream => _opMediator.MsgOpsStream;
        public bool IsConnected => _connection != null && _connection.IsConnected && _connection.CanRead;

        public NatsClient(ConnectionInfo connectionInfo, ISocketFactory socketFactory = null)
        {
            EnsureArg.IsNotNull(connectionInfo, nameof(connectionInfo));

            _sync = new Locker();
            _connectionInfo = connectionInfo.Clone();
            _subscriptions = new ConcurrentDictionary<string, Subscription>();
            _eventMediator = new NatsObservableOf<IClientEvent>();
            _opMediator = new NatsOpMediator();
            _connectionManager = new NatsConnectionManager(socketFactory ?? new SocketFactory());

            Events.SubscribeSafe(ev =>
            {
                if (ev is ClientDisconnected disconnected)
                {
                    var shouldTryToReconnect = _connectionInfo.AutoReconnectOnFailure && disconnected.Reason == DisconnectReason.DueToFailure;
                    if (!shouldTryToReconnect)
                        return;

                    Reconnect();
                }
            });
        }

        private void Reconnect()
        {
            try
            {
                for (var attempts = 0; !IsConnected && attempts < MaxReconnectDueToFailureAttempts; attempts++)
                {
                    _logger.Debug($"Trying to reconnect after disconnection due to failure. attempt={attempts.ToString()}");
                    Connect();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed while trying to reconnect client.", ex);
            }

            if (!IsConnected)
                _eventMediator.Emit(new ClientAutoReconnectFailed(this));
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

            DoSafeRelease();

            Try.All(
                () =>
                {
                    var subscriptions = _subscriptions.Values.Cast<IDisposable>().ToArray();
                    _subscriptions.Clear();
                    Try.DisposeAll(subscriptions);
                },
                () =>
                {
                    Try.DisposeAll(_eventMediator, _opMediator, _sync);
                    _eventMediator = null;
                    _opMediator = null;
                    _sync = null;
                });
        }

        public void Connect()
        {
            ThrowIfDisposed();

            if (IsConnected)
                return;

            using (_sync.Lock())
            {
                if (IsConnected)
                    return;

                DoSafeRelease();

                _cancellation = new CancellationTokenSource();

                var connectionResult = _connectionManager.OpenConnection(
                    _connectionInfo,
                    _cancellation.Token);

                _connection = connectionResult.Item1;

                var opsReceivedWhileConnecting = connectionResult.Item2;

                _eventMediator.Emit(new ClientConnected(this));

                foreach (var op in opsReceivedWhileConnecting)
                    _opMediator.Emit(op);

                foreach (var subscription in _subscriptions.Values)
                {
                    if (!IsConnected)
                        break;

                    DoSub(subscription.SubscriptionInfo);
                }

                _consumer = Task.Factory
                    .StartNew(
                        Worker,
                        _cancellation.Token,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Default)
                    .ContinueWith(t =>
                    {
                        if (!t.IsFaulted)
                            return;

                        DoSafeRelease();

                        _eventMediator.Emit(new ClientDisconnected(this, DisconnectReason.DueToFailure));

                        var ex = t.Exception?.GetBaseException() ?? t.Exception;
                        if (ex == null)
                            return;

                        _logger.Error("Internal client worker exception.", ex);

                        _eventMediator.Emit(new ClientWorkerFailed(this, ex));
                    });
            }
        }

        private void Worker()
        {
            while (
                _cancellation != null &&
                !_cancellation.IsCancellationRequested &&
                IsConnected)
            {
                _logger.Trace("Worker cycle.");

                var lastOpReceivedAt = DateTime.UtcNow;

                try
                {
                    foreach (var op in _connection.ReadOp())
                    {
                        lastOpReceivedAt = DateTime.UtcNow;

                        _opMediator.Emit(op);

                        if (op is ErrOp errOp)
                            throw NatsException.ClientReceivedErrOp(errOp);

                        if (op is PingOp && _connectionInfo.AutoRespondToPing)
                            Pong();
                    }
                }
                catch (IOException ioex)
                {
                    _logger.Error("Worker task got IOException.", ioex);

                    if (_cancellation?.IsCancellationRequested == true)
                        break;

                    if (ioex.InnerException is SocketException socketEx)
                    {
                        _logger.Error($"Worker task got SocketException with SocketErrorCode='{socketEx.SocketErrorCode}'");

                        if (socketEx.SocketErrorCode == SocketError.Interrupted)
                            break;

                        if (socketEx.SocketErrorCode != SocketError.TimedOut)
                            throw;
                    }

                    var silenceDeltaMs = DateTime.UtcNow.Subtract(lastOpReceivedAt).TotalMilliseconds;
                    if (silenceDeltaMs >= ConsumerMaxMsSilenceFromServer)
                        throw NatsException.ConnectionFoundIdling(_connection.ServerInfo.Host, _connection.ServerInfo.Port);

                    if (silenceDeltaMs >= ConsumerPingAfterMsSilenceFromServer)
                        Ping();
                }
            }
        }

        public void Disconnect()
        {
            ThrowIfDisposed();

            DoSafeRelease();

            _eventMediator.Emit(new ClientDisconnected(this, DisconnectReason.ByUser));
        }

        private void DoSafeRelease() => Swallow.Everything(
            () =>
            {
                if (_cancellation == null)
                    return;

                if (!_cancellation.IsCancellationRequested)
                    _cancellation.Cancel(false);

                _cancellation = null;
            },
            () =>
            {
                if (_consumer == null)
                    return;

                if (!_consumer.IsCompleted)
                    _consumer.Wait(WaitForConsumerCompleteMs);

                _consumer = null;
            },
            () =>
            {
                _connection?.Dispose();
                _connection = null;
            });

        public void Flush()
        {
            ThrowIfDisposed();

            _connection.WithWriteLock(writer => writer.Flush());
        }

        public async Task FlushAsync()
        {
            ThrowIfDisposed();

            await _connection.WithWriteLockAsync(
                async writer => await writer.FlushAsync().ConfigureAwait(false)
            ).ConfigureAwait(false);
        }

        public void Ping()
        {
            ThrowIfDisposed();

            var cmdPayload = PingCmd.Generate();

            _connection.WithWriteLock(writer =>
            {
                writer.Write(cmdPayload);
                writer.Flush();
            });
        }

        public async Task PingAsync()
        {
            ThrowIfDisposed();

            var cmdPayload = PingCmd.Generate();

            await _connection.WithWriteLockAsync(async writer =>
            {
                await writer.WriteAsync(cmdPayload).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public void Pong()
        {
            ThrowIfDisposed();

            var cmdPayload = PongCmd.Generate();

            _connection.WithWriteLock(writer =>
            {
                writer.Write(cmdPayload);
                writer.Flush();
            });
        }

        public async Task PongAsync()
        {
            ThrowIfDisposed();

            var cmdPayload = PongCmd.Generate();

            await _connection.WithWriteLockAsync(async writer =>
            {
                await writer.WriteAsync(cmdPayload).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public void Pub(string subject, string body, string replyTo = null)
        {
            ThrowIfDisposed();

            var cmdPayload = PubCmd.Generate(subject, body, replyTo);

            _connection.WithWriteLock(writer =>
            {
                writer.Write(cmdPayload);
                if (ShouldAutoFlush)
                    writer.Flush();
            });
        }

        public void Pub(string subject, byte[] body, string replyTo = null)
        {
            ThrowIfDisposed();

            var cmdPayload = PubCmd.Generate(subject, body, replyTo);

            _connection.WithWriteLock(writer =>
             {
                 writer.Write(cmdPayload);
                 if (ShouldAutoFlush)
                     writer.Flush();
             });
        }

        public void Pub(string subject, IPayload body, string replyTo = null)
        {
            ThrowIfDisposed();

            var cmdPayload = PubCmd.Generate(subject, body, replyTo);

            _connection.WithWriteLock(writer =>
            {
                writer.Write(cmdPayload);
                if (ShouldAutoFlush)
                    writer.Flush();
            });
        }

        public async Task PubAsync(string subject, byte[] body, string replyTo = null)
        {
            ThrowIfDisposed();

            var cmdPayload = PubCmd.Generate(subject, body, replyTo);

            await _connection.WithWriteLockAsync(async writer =>
            {
                await writer.WriteAsync(cmdPayload).ConfigureAwait(false);
                if (ShouldAutoFlush)
                    await writer.FlushAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public async Task PubAsync(string subject, IPayload body, string replyTo = null)
        {
            ThrowIfDisposed();

            var cmdPayload = PubCmd.Generate(subject, body, replyTo);

            await _connection.WithWriteLockAsync(async writer =>
            {
                await writer.WriteAsync(cmdPayload).ConfigureAwait(false);
                if (ShouldAutoFlush)
                    await writer.FlushAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public async Task PubAsync(string subject, string body, string replyTo = null)
        {
            ThrowIfDisposed();

            var cmdPayload = PubCmd.Generate(subject, body, replyTo);

            await _connection.WithWriteLockAsync(async writer =>
            {
                await writer.WriteAsync(cmdPayload).ConfigureAwait(false);
                if (ShouldAutoFlush)
                    await writer.FlushAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public void PubMany(Action<IPublisher> p)
        {
            ThrowIfDisposed();

            _connection.WithWriteLock(writer =>
            {
                p(new Publisher(writer.Write, writer.WriteAsync, writer.Write, writer.WriteAsync));
                if (ShouldAutoFlush)
                    writer.Flush();
            });
        }

        public async Task<MsgOp> RequestAsync(string subject, string body, int? timeoutMs = null)
        {
            ThrowIfDisposed();

            EnsureArg.IsNotNullOrWhiteSpace(subject, nameof(subject));
            EnsureArg.IsNotNullOrWhiteSpace(body, nameof(body));

            return await DoRequestAsync(subject, NatsEncoder.GetBytes(body), timeoutMs).ConfigureAwait(false);
        }

        public async Task<MsgOp> RequestAsync(string subject, byte[] body, int? timeoutMs = null)
        {
            ThrowIfDisposed();

            EnsureArg.IsNotNullOrWhiteSpace(subject, nameof(subject));
            EnsureArg.HasItems(body, nameof(body));

            return await DoRequestAsync(subject, body, timeoutMs).ConfigureAwait(false);
        }

        public async Task<MsgOp> RequestAsync(string subject, IPayload body, int? timeoutMs = null)
        {
            ThrowIfDisposed();

            EnsureArg.IsNotNullOrWhiteSpace(subject, nameof(subject));
            EnsureArg.IsNotNull(body, nameof(body));

            return await DoRequestAsync(subject, body.GetBytes().ToArray(), timeoutMs).ConfigureAwait(false);
        }

        private async Task<MsgOp> DoRequestAsync(string subject, byte[] body, int? timeoutMs)
        {
            var requestReplyAddress = $"{Guid.NewGuid():N}";
            var pubCmd = PubCmd.Generate(subject, body, requestReplyAddress);

            var taskComp = new TaskCompletionSource<MsgOp>();
            var requestSubscription = MsgOpStream.Where(msg => msg.Subject == requestReplyAddress).Subscribe(
                msg => taskComp.SetResult(msg),
                ex => taskComp.SetException(ex));

            var subscriptionInfo = new SubscriptionInfo(requestReplyAddress, maxMessages: 1);
            var subCmd = SubCmd.Generate(subscriptionInfo.Subject, subscriptionInfo.Id);
            var unsubCmd = UnsubCmd.Generate(subscriptionInfo.Id, subscriptionInfo.MaxMessages);

            await _connection.WithWriteLockAsync(async writer =>
            {
                await writer.WriteAsync(subCmd).ConfigureAwait(false);
                await writer.WriteAsync(unsubCmd).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
                await writer.WriteAsync(pubCmd).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);

            Task.WaitAny(new[] { Task.Delay(timeoutMs ?? _connectionInfo.RequestTimeoutMs), taskComp.Task }, _cancellation.Token);
            if (!taskComp.Task.IsCompleted)
                taskComp.SetException(NatsException.RequestTimedOut());

            return await taskComp.Task
                .ContinueWith(t =>
                {
                    requestSubscription?.Dispose();

                    if (!t.IsFaulted)
                        return t.Result;

                    var ex = t.Exception?.GetBaseException() ?? t.Exception;
                    if (ex == null)
                        return t.Result;

                    _logger.Error("Exception while performing request.", ex);

                    throw ex;
                })
                .ConfigureAwait(false);
        }

        public ISubscription Sub(string subject)
            => Sub(new SubscriptionInfo(subject));

        public ISubscription Sub(string subject, Func<INatsObservable<MsgOp>, IDisposable> subscriptionFactory)
            => Sub(new SubscriptionInfo(subject), subscriptionFactory);

        public ISubscription Sub(SubscriptionInfo subscriptionInfo)
            => Sub(subscriptionInfo, msgs => Disposable.Empty);

        public ISubscription Sub(SubscriptionInfo subscriptionInfo, Func<INatsObservable<MsgOp>, IDisposable> subscriptionFactory)
        {
            ThrowIfDisposed();

            var subscription = CreateMsgOpSubscription(subscriptionInfo, subscriptionFactory);

            if (!IsConnected)
                return subscription;

            DoSub(subscriptionInfo);

            return subscription;
        }

        private void DoSub(SubscriptionInfo subscriptionInfo) => _connection.WithWriteLock(writer =>
        {
            writer.Write(SubCmd.Generate(subscriptionInfo.Subject, subscriptionInfo.Id, subscriptionInfo.QueueGroup));

            if (subscriptionInfo.MaxMessages.HasValue)
                writer.Write(UnsubCmd.Generate(subscriptionInfo.Id, subscriptionInfo.MaxMessages));

            writer.Flush();
        });

        public Task<ISubscription> SubAsync(string subject)
            => SubAsync(new SubscriptionInfo(subject));

        public Task<ISubscription> SubAsync(string subject, Func<INatsObservable<MsgOp>, IDisposable> subscriptionFactory)
            => SubAsync(new SubscriptionInfo(subject), subscriptionFactory);

        public Task<ISubscription> SubAsync(SubscriptionInfo subscriptionInfo)
            => SubAsync(subscriptionInfo, msgs => Disposable.Empty);

        public async Task<ISubscription> SubAsync(SubscriptionInfo subscriptionInfo, Func<INatsObservable<MsgOp>, IDisposable> subscriptionFactory)
        {
            ThrowIfDisposed();

            var subscription = CreateMsgOpSubscription(subscriptionInfo, subscriptionFactory);

            if (!IsConnected)
                return subscription;

            await DoSubAsync(subscriptionInfo).ConfigureAwait(false);

            return subscription;
        }

        private async Task DoSubAsync(SubscriptionInfo subscriptionInfo) => await _connection.WithWriteLockAsync(async writer =>
        {
            await writer.WriteAsync(SubCmd.Generate(subscriptionInfo.Subject, subscriptionInfo.Id, subscriptionInfo.QueueGroup)).ConfigureAwait(false);

            if (subscriptionInfo.MaxMessages.HasValue)
                await writer.WriteAsync(UnsubCmd.Generate(subscriptionInfo.Id, subscriptionInfo.MaxMessages)).ConfigureAwait(false);

            await writer.FlushAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);

        private Subscription CreateMsgOpSubscription(SubscriptionInfo subscriptionInfo, Func<INatsObservable<MsgOp>, IDisposable> subscriptionFactory)
        {
            var subscription = Subscription.Create(
                subscriptionInfo,
                subscriptionFactory(MsgOpStream.Where(msg => subscriptionInfo.Matches(msg.Subject))),
                info => Swallow.Everything(() => Unsub(info)));

            if (!_subscriptions.TryAdd(subscription.SubscriptionInfo.Id, subscription))
                throw NatsException.CouldNotCreateSubscription(subscription.SubscriptionInfo);

            return subscription;
        }

        public void Unsub(ISubscription subscription)
        {
            EnsureArg.IsNotNull(subscription, nameof(subscription));

            Unsub(subscription.SubscriptionInfo);
        }

        public void Unsub(SubscriptionInfo subscriptionInfo)
        {
            EnsureArg.IsNotNull(subscriptionInfo, nameof(subscriptionInfo));

            ThrowIfDisposed();

            if (!TryRemoveSubscription(subscriptionInfo))
                return;

            if (!IsConnected)
                return;

            var cmdPayload = UnsubCmd.Generate(subscriptionInfo.Id, subscriptionInfo.MaxMessages);

            _connection.WithWriteLock(writer =>
            {
                if (!IsConnected)
                    return;

                writer.Write(cmdPayload);
                writer.Flush();
            });
        }

        public async Task UnsubAsync(ISubscription subscription)
        {
            EnsureArg.IsNotNull(subscription, nameof(subscription));

            await UnsubAsync(subscription.SubscriptionInfo).ConfigureAwait(false);
        }

        public async Task UnsubAsync(SubscriptionInfo subscriptionInfo)
        {
            EnsureArg.IsNotNull(subscriptionInfo, nameof(subscriptionInfo));

            ThrowIfDisposed();

            if (!TryRemoveSubscription(subscriptionInfo))
                return;

            if (!IsConnected)
                return;

            var cmdPayload = UnsubCmd.Generate(subscriptionInfo.Id, subscriptionInfo.MaxMessages);

            await _connection.WithWriteLockAsync(async writer =>
            {
                if (!IsConnected)
                    return;

                await writer.WriteAsync(cmdPayload).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private bool TryRemoveSubscription(SubscriptionInfo subscriptionInfo)
        {
            _subscriptions.TryRemove(subscriptionInfo.Id, out var tmp);

            return tmp != null;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}