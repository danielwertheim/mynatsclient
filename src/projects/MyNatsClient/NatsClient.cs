using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Linq;
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

        //TODO: Make available as options
        private const int ConsumerPingAfterMsSilenceFromServer = 20000;
        private const int ConsumerMaxMsSilenceFromServer = 40000;
        private const int MaxReconnectDueToFailureAttempts = 5;
        private const int WaitForConsumerCompleteMs = 100;

        private readonly ConnectionInfo _connectionInfo;
        private readonly ConcurrentDictionary<string, Subscription> _subscriptions;
        private readonly Func<bool> _consumerIsCancelled;
        private readonly INatsConnectionManager _connectionManager;

        private Locker _sync;
        private CancellationTokenSource _cancellation;
        private INatsConnection _connection;
        private Task _consumer;
        private ObservableOf<IClientEvent> _eventMediator;
        private NatsOpMediator _opMediator;
        private bool _isDisposed;

        private bool ShouldAutoFlush => _connectionInfo.PubFlushMode != PubFlushMode.Manual;

        public INatsObservable<IClientEvent> Events => _eventMediator;
        public INatsObservable<IOp> OpStream => _opMediator.AllOpsStream;
        public INatsObservable<MsgOp> MsgOpStream => _opMediator.MsgOpsStream;
        public bool IsConnected => _connection != null && _connection.IsConnected;

        public NatsClient(ConnectionInfo connectionInfo, ISocketFactory socketFactory = null)
        {
            EnsureArg.IsNotNull(connectionInfo, nameof(connectionInfo));

            _sync = new Locker();
            _connectionInfo = connectionInfo.Clone();
            _subscriptions = new ConcurrentDictionary<string, Subscription>();
            _eventMediator = new ObservableOf<IClientEvent>();
            _opMediator = new NatsOpMediator();
            _connectionManager = new NatsConnectionManager(socketFactory ?? new SocketFactory());
            _consumerIsCancelled = () => _cancellation == null || _cancellation.IsCancellationRequested;

            Events.Subscribe(new AnonymousObserver<IClientEvent>(ev =>
            {
                if (ev is ClientConnected)
                {
                    foreach (var subscription in _subscriptions.Values)
                    {
                        if (!IsConnected)
                            break;

                        DoSub(subscription.SubscriptionInfo);
                    }
                    return;
                }

                var disconnectedEvent = ev as ClientDisconnected;
                var shouldTryToReconnect = disconnectedEvent?.Reason == DisconnectReason.DueToFailure && _connectionInfo.AutoReconnectOnFailure;
                if (!shouldTryToReconnect)
                    return;

                Reconnect();
            }));
        }

        private void Reconnect()
        {
            try
            {
                for (var attempts = 0; !IsConnected && attempts < MaxReconnectDueToFailureAttempts; attempts++)
                {
                    Logger.Debug($"Trying to reconnect after disconnection due to failure. attempt={attempts.ToString()}");
                    Connect();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed while trying to reconnect client.", ex);
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

            Release();

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

                Release();

                _cancellation = new CancellationTokenSource();

                var connectionResult = _connectionManager.OpenConnection(
                    _connectionInfo,
                    _cancellation.Token);

                _connection = connectionResult.Item1;

                var opsReceivedWhileConnecting = connectionResult.Item2;
                if (opsReceivedWhileConnecting.Any())
                    foreach (var op in opsReceivedWhileConnecting)
                        _opMediator.Emit(op);

                _consumer = Task.Factory
                    .StartNew(
                        Consumer,
                        _cancellation.Token,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Default)
                    .ContinueWith(t =>
                    {
                        if (!t.IsFaulted)
                            return;

                        Disconnect(DisconnectReason.DueToFailure);

                        var ex = t.Exception?.GetBaseException() ?? t.Exception;
                        if (ex == null)
                            return;

                        Logger.Error("Consumer exception.", ex);

                        RaiseClientConsumerFailed(ex);
                    });
            }

            if (IsConnected)
                RaiseClientConnected();
        }

        private void Consumer()
        {
            ErrOp errOp = null;

            bool ShouldRead() => _connection != null && _connection.CanRead && !_consumerIsCancelled();

            while (ShouldRead())
            {
                Logger.Trace("Consume cycle");

                var lastOpReceivedAt = DateTime.UtcNow;

                try
                {
                    foreach (var op in _connection.ReadOp())
                    {
                        if (op == null)
                            continue;

                        errOp = op as ErrOp;
                        if (errOp != null)
                            break;

                        lastOpReceivedAt = DateTime.UtcNow;

                        if (op is PingOp && _connectionInfo.AutoRespondToPing)
                        {
                            Pong();
                            return;
                        }

                        _opMediator.Emit(op);
                    }
                }
                catch (IOException ioex)
                {
                    Logger.Error("Consumer got IOException.", ioex);

                    if (!ShouldRead())
                        break;

                    if (ioex.InnerException is SocketException socketEx)
                    {
                        Logger.Error($"Consumer got SocketException with SocketErrorCode='{socketEx.SocketErrorCode}'");

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

            if (errOp != null)
            {
                Logger.Error($"Consumer stopped with ErrOp with message='{errOp.Message}'.");
                _opMediator.Emit(errOp);
            }
        }

        public void Disconnect()
        {
            ThrowIfDisposed();

            Disconnect(DisconnectReason.ByUser);
        }

        private void Disconnect(DisconnectReason reason)
        {
            if (IsConnected)
            {
                using (_sync.Lock())
                {
                    if (IsConnected)
                        Release();
                }
            }

            RaiseClientDisconnected(reason);
        }

        private void Release()
        {
            Swallow.Everything(
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
        }

        public void Flush()
        {
            ThrowIfDisposed();

            _connection.WithWriteLock(writer => writer.Flush());
        }

        public async Task FlushAsync()
        {
            ThrowIfDisposed();

            await _connection.WithWriteLockAsync(
                async writer => await writer.FlushAsync().ForAwait()
            ).ForAwait();
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
                await writer.WriteAsync(cmdPayload).ForAwait();
                await writer.FlushAsync().ForAwait();
            }).ForAwait();
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
                await writer.WriteAsync(cmdPayload).ForAwait();
                await writer.FlushAsync().ForAwait();
            }).ForAwait();
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
                await writer.WriteAsync(cmdPayload).ForAwait();
                if (ShouldAutoFlush)
                    await writer.FlushAsync().ForAwait();
            }).ForAwait();
        }

        public async Task PubAsync(string subject, IPayload body, string replyTo = null)
        {
            ThrowIfDisposed();

            var cmdPayload = PubCmd.Generate(subject, body, replyTo);

            await _connection.WithWriteLockAsync(async writer =>
            {
                await writer.WriteAsync(cmdPayload).ForAwait();
                if (ShouldAutoFlush)
                    await writer.FlushAsync().ForAwait();
            }).ForAwait();
        }

        public async Task PubAsync(string subject, string body, string replyTo = null)
        {
            ThrowIfDisposed();

            var cmdPayload = PubCmd.Generate(subject, body, replyTo);

            await _connection.WithWriteLockAsync(async writer =>
            {
                await writer.WriteAsync(cmdPayload).ForAwait();
                if (ShouldAutoFlush)
                    await writer.FlushAsync().ForAwait();
            }).ForAwait();
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

            return await DoRequestAsync(subject, NatsEncoder.GetBytes(body), timeoutMs).ForAwait();
        }

        public async Task<MsgOp> RequestAsync(string subject, byte[] body, int? timeoutMs = null)
        {
            ThrowIfDisposed();

            EnsureArg.IsNotNullOrWhiteSpace(subject, nameof(subject));
            EnsureArg.HasItems(body, nameof(body));

            return await DoRequestAsync(subject, body, timeoutMs).ForAwait();
        }

        public async Task<MsgOp> RequestAsync(string subject, IPayload body, int? timeoutMs = null)
        {
            ThrowIfDisposed();

            EnsureArg.IsNotNullOrWhiteSpace(subject, nameof(subject));
            EnsureArg.IsNotNull(body, nameof(body));

            return await DoRequestAsync(subject, body.GetBytes().ToArray(), timeoutMs).ForAwait();
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
                await writer.WriteAsync(subCmd).ForAwait();
                await writer.WriteAsync(unsubCmd).ForAwait();
                await writer.FlushAsync().ForAwait();
                await writer.WriteAsync(pubCmd).ForAwait();
                await writer.FlushAsync().ForAwait();
            }).ForAwait();

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

                    Logger.Error("Exception while performing request.", ex);

                    throw ex;
                })
                .ForAwait();
        }

        public ISubscription Sub(string subject)
            => Sub(new SubscriptionInfo(subject));

        public ISubscription Sub(SubscriptionInfo subscriptionInfo)
            => Sub(subscriptionInfo, msgs => Disposable.Empty);

        public ISubscription Sub(string subject, Action<MsgOp> handler)
            => Sub(new SubscriptionInfo(subject), new AnonymousObserver<MsgOp>(handler));

        public ISubscription Sub(string subject, IObserver<MsgOp> observer)
            => Sub(new SubscriptionInfo(subject), observer);

        public ISubscription Sub(string subject, Func<INatsObservable<MsgOp>, IDisposable> subscriptionFactory)
            => Sub(new SubscriptionInfo(subject), subscriptionFactory);

        public ISubscription Sub(SubscriptionInfo subscriptionInfo, Action<MsgOp> handler)
            => Sub(subscriptionInfo, new AnonymousObserver<MsgOp>(handler));

        public ISubscription Sub(SubscriptionInfo subscriptionInfo, IObserver<MsgOp> observer)
            => Sub(subscriptionInfo, msgStream => msgStream.Where(msg => subscriptionInfo.Matches(msg.Subject)).Subscribe(observer));

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

        public Task<ISubscription> SubAsync(string subject, Action<MsgOp> handler)
            => SubAsync(new SubscriptionInfo(subject), new AnonymousObserver<MsgOp>(handler));

        public Task<ISubscription> SubAsync(string subject, IObserver<MsgOp> observer)
            => SubAsync(new SubscriptionInfo(subject), observer);

        public Task<ISubscription> SubAsync(string subject, Func<INatsObservable<MsgOp>, IDisposable> subscriptionFactory)
            => SubAsync(new SubscriptionInfo(subject), subscriptionFactory);

        public Task<ISubscription> SubAsync(SubscriptionInfo subscriptionInfo)
            => SubAsync(subscriptionInfo, msgs => Disposable.Empty);

        public Task<ISubscription> SubAsync(SubscriptionInfo subscriptionInfo, Action<MsgOp> handler)
            => SubAsync(subscriptionInfo, new AnonymousObserver<MsgOp>(handler));

        public Task<ISubscription> SubAsync(SubscriptionInfo subscriptionInfo, IObserver<MsgOp> observer)
            => SubAsync(subscriptionInfo, msgStream => msgStream.Where(msg => subscriptionInfo.Matches(msg.Subject)).Subscribe(observer));

        public async Task<ISubscription> SubAsync(SubscriptionInfo subscriptionInfo, Func<INatsObservable<MsgOp>, IDisposable> subscriptionFactory)
        {
            ThrowIfDisposed();

            var subscription = CreateMsgOpSubscription(subscriptionInfo, subscriptionFactory);

            if (!IsConnected)
                return subscription;

            await DoSubAsync(subscriptionInfo).ForAwait();

            return subscription;
        }

        private async Task DoSubAsync(SubscriptionInfo subscriptionInfo) => await _connection.WithWriteLockAsync(async writer =>
        {
            await writer.WriteAsync(SubCmd.Generate(subscriptionInfo.Subject, subscriptionInfo.Id, subscriptionInfo.QueueGroup)).ForAwait();

            if (subscriptionInfo.MaxMessages.HasValue)
                await writer.WriteAsync(UnsubCmd.Generate(subscriptionInfo.Id, subscriptionInfo.MaxMessages)).ForAwait();

            await writer.FlushAsync().ForAwait();
        }).ForAwait();

        private Subscription CreateMsgOpSubscription(SubscriptionInfo subscriptionInfo, Func<INatsObservable<MsgOp>, IDisposable> subscriptionFactory)
        {
            var subscription = Subscription.Create(subscriptionInfo, subscriptionFactory(MsgOpStream), info =>
            {
                if (!TryRemoveSubscription(info))
                    return;

                Swallow.Everything(() => Unsub(info));
            });

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

                await writer.WriteAsync(cmdPayload).ForAwait();
                await writer.FlushAsync().ForAwait();
            }).ForAwait();
        }

        private bool TryRemoveSubscription(SubscriptionInfo subscriptionInfo)
        {
            Subscription tmp;

            _subscriptions.TryRemove(subscriptionInfo.Id, out tmp);

            return tmp != null;
        }

        private void RaiseClientConnected()
            => _eventMediator.Emit(new ClientConnected(this));

        private void RaiseClientDisconnected(DisconnectReason reason)
            => _eventMediator.Emit(new ClientDisconnected(this, reason));

        private void RaiseClientConsumerFailed(Exception ex)
            => _eventMediator.Emit(new ClientConsumerFailed(this, ex));

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}