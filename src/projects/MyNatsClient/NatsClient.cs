using System;
using System.Collections.Concurrent;
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
        private const int MaxReconnectDueToFailureAttempts = 5;
        private const int WaitForConsumerCompleteMs = 100;
        private const bool AutoRemoveFailingObservableSubscription = false;

        private readonly object _sync;
        private readonly ConnectionInfo _connectionInfo;
        private readonly ConcurrentDictionary<string, Subscription> _subscriptions;
        private readonly Func<bool> _consumerIsCancelled;

        private CancellationTokenSource _cancellation;
        private INatsConnection _connection;
        private Task _consumer;
        private ObservableOf<IClientEvent> _eventMediator;
        private NatsOpMediator _opMediator;
        private Lazy<ClientInbox> _inbox;
        private bool _isDisposed;

        private bool ShouldAutoFlush => _connectionInfo.PubFlushMode != PubFlushMode.Manual;

        public string Id { get; }
        public IObservable<IClientEvent> Events => _eventMediator;
        public IFilterableObservable<IOp> OpStream => _opMediator.AllOpsStream;
        public IFilterableObservable<MsgOp> MsgOpStream => _opMediator.MsgOpsStream;
        public INatsClientStats Stats => _opMediator;
        public bool IsConnected => _connection != null && _connection.IsConnected;
        public INatsConnectionManager ConnectionManager { private get; set; }
        private ClientInbox Inbox => _inbox.Value;

        public NatsClient(string id, ConnectionInfo connectionInfo)
        {
            _sync = new object();
            _connectionInfo = connectionInfo.Clone();
            _subscriptions = new ConcurrentDictionary<string, Subscription>();
            _eventMediator = new ObservableOf<IClientEvent>(AutoRemoveFailingObservableSubscription);
            _opMediator = new NatsOpMediator(AutoRemoveFailingObservableSubscription);
            _inbox = new Lazy<ClientInbox>(() => new ClientInbox(this), LazyThreadSafetyMode.ExecutionAndPublication);
            _consumerIsCancelled = () => _cancellation == null || _cancellation.IsCancellationRequested;

            Id = id;
            ConnectionManager = new NatsConnectionManager(new SocketFactory());

            SubscribeToClientEventsForInternalUse();

            _opMediator.AllOpsStream.Subscribe(new DelegatingObserver<IOp>(op =>
            {
                if (!IsConnected)
                    return;

                if (_connectionInfo.AutoRespondToPing && op is PingOp)
                    Swallow.Everything(Pong);
            }));
        }

        private void SubscribeToClientEventsForInternalUse()
        {
            Events.Subscribe(new DelegatingObserver<IClientEvent>(ev =>
            {
                if (ev is ClientConnected)
                {
                    HandleClientConnected();
                    return;
                }

                var disconnectedEvent = ev as ClientDisconnected;
                if (disconnectedEvent != null)
                {
                    HandleClientDisconnected(disconnectedEvent);
                    // ReSharper disable once RedundantJumpStatement
                    return;
                }
            }));
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
                    if (_inbox == null)
                        return;

                    _inbox.Value.Dispose();
                    _inbox = null;
                },
                () =>
                {
                    var subscriptions = _subscriptions.Values.Cast<IDisposable>().ToArray();
                    _subscriptions.Clear();
                    Try.DisposeAll(subscriptions);
                },
                () =>
                {
                    Try.DisposeAll(_eventMediator, _opMediator);
                    _eventMediator = null;
                    _opMediator = null;
                });
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
                lock (_sync)
                {
                    if (IsConnected)
                        Release();
                }
            }

            RaiseClientDisconnected(reason);
        }

        public void Connect()
        {
            ThrowIfDisposed();

            if (IsConnected)
                return;

            lock (_sync)
            {
                if (IsConnected)
                    return;

                Release();

                _cancellation = new CancellationTokenSource();

                var connectionResult = ConnectionManager.OpenConnection(_connectionInfo, _cancellation.Token);
                _connection = connectionResult.Item1;

                var ops = connectionResult.Item2;
                if (ops.Any())
                    foreach (var op in ops)
                        _opMediator.Dispatch(op);

                _consumer = Task.Factory.StartNew(
                    Consumer,
                    _cancellation.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default).ContinueWith(OnConsumerCompleted);
            }

            if (IsConnected)
                RaiseClientConnected();
        }

        private void Consumer()
        {
            ErrOp errOp = null;

            Func<bool> shouldRead = () =>
                _connection != null &&
                _connection.CanRead &&
                !_consumerIsCancelled();

            while (shouldRead())
            {
                try
                {
                    foreach (var op in _connection.ReadOp())
                    {
                        if (op == null)
                            continue;

                        errOp = op as ErrOp;
                        if (errOp != null)
                            break;

                        _opMediator.Dispatch(op);
                    }
                }
                catch (IOException ioex)
                {
                    if (!shouldRead())
                        break;

                    var ex = ioex.InnerException as SocketException;
                    if (ex != null)
                    {
                        if (ex.SocketErrorCode == SocketError.Interrupted)
                            break;

                        if (ex.SocketErrorCode != SocketError.TimedOut)
                            throw;
                    }

                    var silenceDeltaMs = DateTime.UtcNow.Subtract(Stats.LastOpReceivedAt).TotalMilliseconds;
                    if (silenceDeltaMs >= ConsumerMaxMsSilenceFromServer)
                        throw NatsException.ConnectionFoundIdling(_connection.ServerInfo.Host, _connection.ServerInfo.Port);

                    if (silenceDeltaMs >= ConsumerPingAfterMsSilenceFromServer)
                        Ping();
                }
            }

            if (errOp != null)
                _opMediator.Dispatch(errOp);
        }

        private void OnConsumerCompleted(Task t)
        {
            if (!t.IsFaulted)
                return;

            Disconnect(DisconnectReason.DueToFailure);

            var ex = t.Exception?.GetBaseException() ?? t.Exception;
            if (ex == null)
                return;

            Logger.Error("Consumer exception.", ex);
            RaiseClientConsumerFailed(ex);
        }

        private void Release()
        {
            lock (_sync)
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
        }

        public void Ping()
        {
            ThrowIfDisposed();

            _connection.WithWriteLock(writer =>
            {
                writer.Write(PingCmd.Generate());
                writer.Flush();
            });
        }

        public async Task PingAsync()
        {
            ThrowIfDisposed();

            await _connection.WithWriteLockAsync(async writer =>
            {
                await writer.WriteAsync(PingCmd.Generate()).ForAwait();
                await writer.FlushAsync().ForAwait();
            }).ForAwait();
        }

        public void Pong()
        {
            ThrowIfDisposed();

            _connection.WithWriteLock(writer =>
            {
                writer.Write(PongCmd.Generate());
                writer.Flush();
            });
        }

        public async Task PongAsync()
        {
            ThrowIfDisposed();

            await _connection.WithWriteLockAsync(async writer =>
            {
                await writer.WriteAsync(PongCmd.Generate()).ForAwait();
                await writer.FlushAsync().ForAwait();
            }).ForAwait();
        }

        public void Pub(string subject, string body, string replyTo = null)
        {
            ThrowIfDisposed();

            _connection.WithWriteLock(writer =>
            {
                writer.Write(PubCmd.Generate(subject, body, replyTo));
                if (ShouldAutoFlush)
                    writer.Flush();
            });
        }

        public void Pub(string subject, byte[] body, string replyTo = null)
        {
            ThrowIfDisposed();

            _connection.WithWriteLock(writer =>
             {
                 writer.Write(PubCmd.Generate(subject, body, replyTo));
                 if (ShouldAutoFlush)
                     writer.Flush();
             });
        }

        public void Pub(string subject, IPayload body, string replyTo = null)
        {
            ThrowIfDisposed();

            _connection.WithWriteLock(writer =>
            {
                writer.Write(PubCmd.Generate(subject, body, replyTo));
                if (ShouldAutoFlush)
                    writer.Flush();
            });
        }

        public async Task PubAsync(string subject, byte[] body, string replyTo = null)
        {
            ThrowIfDisposed();

            await _connection.WithWriteLockAsync(async writer =>
            {
                await writer.WriteAsync(PubCmd.Generate(subject, body, replyTo)).ForAwait();
                if (ShouldAutoFlush)
                    await writer.FlushAsync().ForAwait();
            }).ForAwait();
        }

        public async Task PubAsync(string subject, IPayload body, string replyTo = null)
        {
            ThrowIfDisposed();

            await _connection.WithWriteLockAsync(async writer =>
            {
                await writer.WriteAsync(PubCmd.Generate(subject, body, replyTo)).ForAwait();
                if (ShouldAutoFlush)
                    await writer.FlushAsync().ForAwait();
            }).ForAwait();
        }

        public async Task PubAsync(string subject, string body, string replyTo = null)
        {
            ThrowIfDisposed();

            await _connection.WithWriteLockAsync(async writer =>
            {
                await writer.WriteAsync(PubCmd.Generate(subject, body, replyTo)).ForAwait();
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

            return await DoRequestAsync(subject, body.Blocks.SelectMany(b => b).ToArray(), timeoutMs).ForAwait();
        }

        private async Task<MsgOp> DoRequestAsync(string subject, byte[] body, int? timeoutMs)
        {
            var requestReplyAddress = $"{Inbox.Address}.{Guid.NewGuid():N}";
            var taskComp = new TaskCompletionSource<MsgOp>();
            var requestSubscription = Inbox.Responses.Subscribe(
                new DelegatingObserver<MsgOp>(msg => taskComp.SetResult(msg), ex => taskComp.SetException(ex)),
                msg => msg.Subject == requestReplyAddress);

            await _connection.WithWriteLockAsync(async writer =>
            {
                await writer.WriteAsync(PubCmd.Generate(subject, body, requestReplyAddress)).ForAwait();
                await writer.FlushAsync().ForAwait();
            }).ForAwait();

            Task.WaitAny(new[] { Task.Delay(timeoutMs ?? _connectionInfo.RequestTimeoutMs), taskComp.Task }, _cancellation.Token);
            if (!taskComp.Task.IsCompleted)
                throw NatsException.RequestTimedOut();

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

        public void Flush()
        {
            ThrowIfDisposed();

            _connection.WithWriteLock(writer => writer.Flush());
        }

        public async Task FlushAsync()
        {
            ThrowIfDisposed();

            await _connection.WithWriteLockAsync(async writer => await writer.FlushAsync().ForAwait()).ForAwait();
        }

        public ISubscription Sub(string subject)
            => Sub(new SubscriptionInfo(subject));

        public ISubscription Sub(SubscriptionInfo subscriptionInfo)
            => SubWithObservableSubscription(subscriptionInfo, msgs => Disposable.Empty);

        public ISubscription SubWithHandler(string subject, Action<MsgOp> handler)
            => SubWithObserver(new SubscriptionInfo(subject), new DelegatingObserver<MsgOp>(handler));

        public ISubscription SubWithObserver(string subject, IObserver<MsgOp> observer)
            => SubWithObserver(new SubscriptionInfo(subject), observer);

        public ISubscription SubWithObservableSubscription(string subject, Func<IFilterableObservable<MsgOp>, IDisposable> subscriptionFactory)
            => SubWithObservableSubscription(new SubscriptionInfo(subject), subscriptionFactory);

        public ISubscription SubWithHandler(SubscriptionInfo subscriptionInfo, Action<MsgOp> handler)
            => SubWithObserver(subscriptionInfo, new DelegatingObserver<MsgOp>(handler));

        public ISubscription SubWithObserver(SubscriptionInfo subscriptionInfo, IObserver<MsgOp> observer)
            => SubWithObservableSubscription(subscriptionInfo, msgStream => msgStream.Subscribe(observer, msg => subscriptionInfo.Matches(msg.Subject)));

        public ISubscription SubWithObservableSubscription(SubscriptionInfo subscriptionInfo, Func<IFilterableObservable<MsgOp>, IDisposable> subscriptionFactory)
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
                writer.Write(UnSubCmd.Generate(subscriptionInfo.Id, subscriptionInfo.MaxMessages));

            writer.Flush();
        });

        public Task<ISubscription> SubAsync(string subject)
            => SubAsync(new SubscriptionInfo(subject));

        public Task<ISubscription> SubWithHandlerAsync(string subject, Action<MsgOp> handler)
            => SubWithObserverAsync(new SubscriptionInfo(subject), new DelegatingObserver<MsgOp>(handler));

        public Task<ISubscription> SubWithObserverAsync(string subject, IObserver<MsgOp> observer)
            => SubWithObserverAsync(new SubscriptionInfo(subject), observer);

        public Task<ISubscription> SubWithObservableSubscriptionAsync(string subject, Func<IFilterableObservable<MsgOp>, IDisposable> subscriptionFactory)
            => SubWithObservableSubscriptionAsync(new SubscriptionInfo(subject), subscriptionFactory);

        public Task<ISubscription> SubAsync(SubscriptionInfo subscriptionInfo)
            => SubWithObservableSubscriptionAsync(subscriptionInfo, msgs => Disposable.Empty);

        public Task<ISubscription> SubWithHandlerAsync(SubscriptionInfo subscriptionInfo, Action<MsgOp> handler)
            => SubWithObserverAsync(subscriptionInfo, new DelegatingObserver<MsgOp>(handler));

        public Task<ISubscription> SubWithObserverAsync(SubscriptionInfo subscriptionInfo, IObserver<MsgOp> observer)
            => SubWithObservableSubscriptionAsync(subscriptionInfo, msgStream => msgStream.Subscribe(observer, msg => subscriptionInfo.Matches(msg.Subject)));

        public async Task<ISubscription> SubWithObservableSubscriptionAsync(SubscriptionInfo subscriptionInfo, Func<IFilterableObservable<MsgOp>, IDisposable> subscriptionFactory)
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
                await writer.WriteAsync(UnSubCmd.Generate(subscriptionInfo.Id, subscriptionInfo.MaxMessages)).ForAwait();

            await writer.FlushAsync().ForAwait();
        }).ForAwait();

        private Subscription CreateMsgOpSubscription(SubscriptionInfo subscriptionInfo, Func<IFilterableObservable<MsgOp>, IDisposable> subscriptionFactory)
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

        public void Unsub(SubscriptionInfo subscriptionInfo)
        {
            EnsureArg.IsNotNull(subscriptionInfo, nameof(subscriptionInfo));

            ThrowIfDisposed();

            if (!TryRemoveSubscription(subscriptionInfo))
                return;

            if (!IsConnected)
                return;

            _connection.WithWriteLock(writer =>
            {
                if (!IsConnected)
                    return;

                writer.Write(UnSubCmd.Generate(subscriptionInfo.Id, subscriptionInfo.MaxMessages));
                writer.Flush();
            });
        }

        public async Task UnsubAsync(SubscriptionInfo subscriptionInfo)
        {
            EnsureArg.IsNotNull(subscriptionInfo, nameof(subscriptionInfo));

            ThrowIfDisposed();

            if (!TryRemoveSubscription(subscriptionInfo))
                return;

            if (!IsConnected)
                return;

            await _connection.WithWriteLockAsync(async writer =>
            {
                if (!IsConnected)
                    return;

                await writer.WriteAsync(UnSubCmd.Generate(subscriptionInfo.Id, subscriptionInfo.MaxMessages)).ForAwait();
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
            => _eventMediator.Dispatch(new ClientConnected(this));

        private void RaiseClientDisconnected(DisconnectReason reason)
            => _eventMediator.Dispatch(new ClientDisconnected(this, reason));

        private void RaiseClientConsumerFailed(Exception ex)
            => _eventMediator.Dispatch(new ClientConsumerFailed(this, ex));

        private void HandleClientConnected()
        {
            foreach (var subscription in _subscriptions.Values)
            {
                if (!IsConnected)
                    break;

                DoSub(subscription.SubscriptionInfo);
            }
        }

        private void HandleClientDisconnected(ClientDisconnected disconnectedEvent)
        {
            if (disconnectedEvent == null || disconnectedEvent.Reason != DisconnectReason.DueToFailure)
                return;

            if (!_connectionInfo.AutoReconnectOnFailure)
                return;

            try
            {
                var attempts = 0;
                while (!IsConnected && attempts < MaxReconnectDueToFailureAttempts)
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

            if (!IsConnected)
                _eventMediator.Dispatch(new ClientAutoReconnectFailed(this));
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}