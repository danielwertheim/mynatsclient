using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyNatsClient.Events;
using MyNatsClient.Internals;
using MyNatsClient.Internals.Commands;
using MyNatsClient.Ops;
using MyNatsClient.Rx;

namespace MyNatsClient
{
    public sealed class NatsClient : INatsClient, IDisposable
    {
        private readonly ILogger<NatsClient> _logger = LoggerManager.CreateLogger<NatsClient>();

        private const int ConsumerPingAfterMsSilenceFromServer = 20000;
        private const int ConsumerMaxMsSilenceFromServer = 40000;
        private const int MaxReconnectDueToFailureAttempts = 5;
        private const int WaitForConsumerCompleteMs = 100;

        private readonly ConnectionInfo _connectionInfo;
        private readonly ConcurrentDictionary<string, Subscription> _subscriptions;
        private readonly INatsConnectionManager _connectionManager;
        private readonly IConsumerFactory _consumerFactory;

        private SemaphoreSlim _sync;
        private CancellationTokenSource _cancellation;
        private INatsConnection _connection;
        private Task _consumer;
        private NatsObservableOf<IClientEvent> _eventMediator;
        private NatsOpMediator _opMediator;
        private bool _isDisposed;

        private bool ShouldAutoFlush => _connectionInfo.PubFlushMode != PubFlushMode.Manual;

        private readonly string _inboxAddress;
        private ISubscription _inboxSubscription;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<MsgOp>> _outstandingRequests = new ConcurrentDictionary<string, TaskCompletionSource<MsgOp>>();

        public string Id { get; }
        public INatsObservable<IClientEvent> Events => _eventMediator;
        public INatsObservable<IOp> OpStream => _opMediator.AllOpsStream;
        public INatsObservable<MsgOp> MsgOpStream => _opMediator.MsgOpsStream;
        public bool IsConnected => _connection != null && _connection.IsConnected && _connection.CanRead;

        public NatsClient(
            ConnectionInfo connectionInfo,
            ISocketFactory socketFactory = null,
            IConsumerFactory consumerFactory = null)
        {
            if (connectionInfo == null)
                throw new ArgumentNullException(nameof(connectionInfo));

            Id = UniqueId.Generate();

            _inboxAddress = $"IB.{Id}";
            _sync = new SemaphoreSlim(1, 1);
            _connectionInfo = connectionInfo.Clone();
            _subscriptions = new ConcurrentDictionary<string, Subscription>(StringComparer.OrdinalIgnoreCase);
            _eventMediator = new NatsObservableOf<IClientEvent>();
            _opMediator = new NatsOpMediator();
            _connectionManager = new NatsConnectionManager(socketFactory ?? new SocketFactory());
            _consumerFactory = consumerFactory ?? new DefaultTaskSchedulerConsumerFactory();

            Events.SubscribeSafe(async ev =>
            {
                switch (ev)
                {
                    case ClientDisconnected disconnected
                        when disconnected.Reason == DisconnectReason.DueToFailure && _connectionInfo.AutoReconnectOnFailure:
                        await ReconnectAsync().ConfigureAwait(false);
                        break;
                }
            });
        }

        private async Task ReconnectAsync()
        {
            for (var attempts = 0; !IsConnected && attempts < MaxReconnectDueToFailureAttempts; attempts++)
            {
                try
                {
                    await ConnectAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed while trying to reconnect client.");
                }
            }

            if (!IsConnected && !_isDisposed)
                _eventMediator.Emit(new ClientAutoReconnectFailed(this));
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        private void ThrowIfNotConnected()
        {
            if (!IsConnected)
                throw NatsException.NotConnected();
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            _isDisposed = true;

            DoSafeRelease();

            var exs = new List<Exception>();

            void TryDispose(IDisposable disposable)
            {
                if (disposable == null)
                    return;

                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    exs.Add(ex);
                }
            }

            TryDispose(_inboxSubscription);

            foreach (var s in _subscriptions.Values)
                TryDispose(s);

            TryDispose(_eventMediator);
            TryDispose(_opMediator);
            TryDispose(_sync);

            _subscriptions.Clear();
            _inboxSubscription = null;
            _eventMediator = null;
            _opMediator = null;
            _sync = null;

            if (exs.Any())
                throw new AggregateException("Failed while disposing client. See inner exception(s) for more details.", exs);
        }

        public async Task ConnectAsync()
        {
            ThrowIfDisposed();

            await _sync.WaitAsync().ConfigureAwait(false);

            try
            {
                if (IsConnected)
                    return;

                DoSafeRelease();

                _cancellation = new CancellationTokenSource();

                IList<IOp> opsReceivedWhileConnecting;

                (_connection, opsReceivedWhileConnecting) =
                    await _connectionManager.OpenConnectionAsync(_connectionInfo, _cancellation.Token).ConfigureAwait(false);

                _consumer = _consumerFactory
                    .Run(ConsumerWork, _cancellation.Token)
                    .ContinueWith(async t =>
                    {
                        if (_isDisposed)
                            return;

                        await _sync.WaitAsync().ConfigureAwait(false);

                        try
                        {
                            if (_isDisposed)
                                return;

                            if (!t.IsFaulted)
                                return;

                            DoSafeRelease();

                            _eventMediator.Emit(new ClientDisconnected(this, DisconnectReason.DueToFailure));

                            var ex = t.Exception?.GetBaseException() ?? t.Exception;
                            if (ex == null)
                                return;

                            _logger.LogError(ex, "Internal client worker exception.");

                            _eventMediator.Emit(new ClientWorkerFailed(this, ex));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Unhandled exception while ending worker.");
                        }
                        finally
                        {
                            _sync?.Release();
                        }
                    });

                _eventMediator.Emit(new ClientConnected(this));

                if (opsReceivedWhileConnecting.Any())
                    _opMediator.Emit(opsReceivedWhileConnecting);

                await DoSubAsync(_subscriptions.Values.Select(i => i.SubscriptionInfo).ToArray()).ConfigureAwait(false);
            }
            finally
            {
                _sync.Release();
            }
        }

        private void ConsumerWork()
        {
            bool ShouldDoWork() => !_isDisposed && IsConnected && _cancellation?.IsCancellationRequested == false;

            var lastOpReceivedAt = DateTime.UtcNow;
            var ping = false;

            while (ShouldDoWork())
            {
                try
                {
                    if (ping)
                    {
                        ping = false;
                        _logger.LogDebug("Pinging due to silent server.");
                        Ping();
                    }

                    foreach (var op in _connection.ReadOp())
                    {
                        lastOpReceivedAt = DateTime.UtcNow;

                        _opMediator.Emit(op);

                        switch (op)
                        {
                            case PingOp _ when _connectionInfo.AutoRespondToPing && ShouldDoWork():
                                Pong();
                                break;
                            case ErrOp errOp:
                                throw NatsException.ClientReceivedErrOp(errOp);
                        }
                    }
                }
                catch (NatsException nex) when (nex.ExceptionCode == NatsExceptionCodes.OpParserError)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (!ShouldDoWork())
                        break;

                    _logger.LogError(ex, "Worker got Exception.");

                    if (ex.InnerException is SocketException socketEx)
                    {
                        _logger.LogError("Worker task got SocketException with SocketErrorCode={SocketErrorCode}", socketEx.SocketErrorCode);

                        if (socketEx.SocketErrorCode == SocketError.Interrupted)
                            break;

                        if (socketEx.SocketErrorCode != SocketError.TimedOut)
                            throw;
                    }

                    var silenceDeltaMs = DateTime.UtcNow.Subtract(lastOpReceivedAt).TotalMilliseconds;
                    if (silenceDeltaMs >= ConsumerMaxMsSilenceFromServer)
                        throw NatsException.ConnectionFoundIdling(_connection.ServerInfo.Host, _connection.ServerInfo.Port);

                    if (silenceDeltaMs >= ConsumerPingAfterMsSilenceFromServer)
                        ping = true;
                }
            }
        }

        public void Disconnect()
        {
            ThrowIfDisposed();

            _sync.Wait();

            try
            {
                DoSafeRelease();
            }
            finally
            {
                _sync?.Release();
            }

            _eventMediator.Emit(new ClientDisconnected(this, DisconnectReason.ByUser));
        }

        private void DoSafeRelease() => Swallow.Everything(
            () =>
            {
                if (_cancellation == null)
                    return;

                if (!_cancellation.IsCancellationRequested)
                    _cancellation.Cancel(false);

                _cancellation.Dispose();
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
                foreach (var key in _outstandingRequests.Keys)
                {
                    if (_outstandingRequests.TryRemove(key, out var tcs))
                    {
                        try
                        {
                            tcs.TrySetCanceled();
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
            },
            () =>
            {
                _connection?.Dispose();
                _connection = null;
            });

        public void Flush()
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            _connection.WithWriteLock(writer => writer.Flush());
        }

        public async Task FlushAsync()
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            await _connection.WithWriteLockAsync(
                async writer => await writer.FlushAsync().ConfigureAwait(false)
            ).ConfigureAwait(false);
        }

        public void Ping()
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            _connection.WithWriteLock(PingCmd.Write);
        }

        public async Task PingAsync()
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            await _connection.WithWriteLockAsync(PingCmd.WriteAsync).ConfigureAwait(false);
        }

        public void Pong()
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            _connection.WithWriteLock(PongCmd.Write);
        }

        public async Task PongAsync()
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            await _connection.WithWriteLockAsync(PongCmd.WriteAsync).ConfigureAwait(false);
        }

        public void Pub(string subject, string body, string replyTo = null)
            => Pub(subject, NatsEncoder.GetBytes(body), replyTo);

        public void Pub(string subject, ReadOnlyMemory<byte> body, string replyTo = null)
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            if (body.Length > _connection.ServerInfo.MaxPayload)
                throw NatsException.ExceededMaxPayload(_connection.ServerInfo.MaxPayload, body.Length);

            _connection.WithWriteLock((writer, arg) =>
            {
                var (subjectIn, bodyIn, replyToIn) = arg;

                PubCmd.Write(writer, subjectIn.Span, replyToIn.Span, bodyIn);
                if (ShouldAutoFlush)
                    writer.Flush();
            }, Tuple.Create(subject.AsMemory(), body, replyTo.AsMemory()));
        }

        public Task PubAsync(string subject, string body, string replyTo = null)
            => PubAsync(subject, NatsEncoder.GetBytes(body), replyTo);

        public async Task PubAsync(string subject, ReadOnlyMemory<byte> body, string replyTo = null)
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            if (body.Length > _connection.ServerInfo.MaxPayload)
                throw NatsException.ExceededMaxPayload(_connection.ServerInfo.MaxPayload, body.Length);

            await _connection.WithWriteLockAsync(async (writer, arg) =>
            {
                var (subjectIn, bodyIn, replyToIn) = arg;
                await PubCmd.WriteAsync(writer, subjectIn, replyToIn, bodyIn).ConfigureAwait(false);
                if (ShouldAutoFlush)
                    await writer.FlushAsync().ConfigureAwait(false);
            }, Tuple.Create(subject.AsMemory(), body, replyTo.AsMemory())).ConfigureAwait(false);
        }

        public void PubMany(Action<IPublisher> p)
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            _connection.WithWriteLock(writer =>
            {
                p(new Publisher(writer, _connection.ServerInfo.MaxPayload));
                if (ShouldAutoFlush)
                    writer.Flush();
            });
        }

        public async Task PubManyAsync(Func<IAsyncPublisher, Task> p)
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            await _connection.WithWriteLockAsync(async writer =>
            {
                await p(new AsyncPublisher(writer, _connection.ServerInfo.MaxPayload)).ConfigureAwait(false);
                if (ShouldAutoFlush)
                    await writer.FlushAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public Task<MsgOp> RequestAsync(string subject, string body, CancellationToken cancellationToken = default)
            => RequestAsync(subject, NatsEncoder.GetBytes(body), cancellationToken);

        public Task<MsgOp> RequestAsync(string subject, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            if (body.Length > _connection.ServerInfo.MaxPayload)
                throw NatsException.ExceededMaxPayload(_connection.ServerInfo.MaxPayload, body.Length);

            return _connectionInfo.UseInboxRequests
                ? DoRequestUsingInboxAsync(subject, body, cancellationToken)
                : DoRequestAsync(subject, body, cancellationToken);
        }

        private async Task<MsgOp> DoRequestAsync(string subject, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default)
        {
            var replyToSubject = UniqueId.Generate();
            var taskComp = new TaskCompletionSource<MsgOp>();
            using var _ = MsgOpStream
                .WhereSubjectMatches(replyToSubject)
                .SubscribeSafe(msg => taskComp.SetResult(msg), ex => taskComp.SetException(ex));

            using var cts = cancellationToken == default
                ? new CancellationTokenSource(_connectionInfo.RequestTimeoutMs)
                : CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token, cancellationToken);

            await using var __ = cts.Token.Register(() => taskComp.SetCanceled()).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            await _connection.WithWriteLockAsync(async (writer, arg) =>
            {
                var (subjectIn, bodyIn, replyToIn) = arg;
                var sid = UniqueId.Generate().AsMemory();

                await SubCmd.WriteAsync(writer, replyToIn, sid, ReadOnlyMemory<char>.Empty).ConfigureAwait(false);
                await UnsubCmd.WriteAsync(writer, sid, 1).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
                await PubCmd.WriteAsync(writer, subjectIn, replyToIn, bodyIn).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }, Tuple.Create(subject.AsMemory(), body, replyToSubject.AsMemory())).ConfigureAwait(false);

            return await taskComp.Task.ConfigureAwait(false);
        }

        private void SetupInboxSubscription()
        {
            _inboxSubscription = CreateSubscription(new SubscriptionInfo($"{_inboxAddress}.*"), messages =>
                messages.SubscribeSafe(
                    msg =>
                    {
                        var lastIndexOf = msg.Subject.LastIndexOf('.');
                        if (lastIndexOf == -1)
                            return;

                        var requestId = msg.Subject.Substring(lastIndexOf + 1);
                        if (_outstandingRequests.TryRemove(requestId, out var ts))
                            ts.TrySetResult(msg);
                    }));
        }

        private async Task<MsgOp> DoRequestUsingInboxAsync(string subject, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default)
        {
            var requestId = UniqueId.Generate();
            var replyToSubject = $"{_inboxAddress}.{requestId}";
            var taskComp = new TaskCompletionSource<MsgOp>();
            if (!_outstandingRequests.TryAdd(requestId, taskComp))
                throw NatsException.InitRequestError("Unable to initiate request.");

            using var cts = cancellationToken == default
                ? new CancellationTokenSource(_connectionInfo.RequestTimeoutMs)
                : CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token, cancellationToken);

            await using var _ = cts.Token.Register(() =>
            {
                if (_outstandingRequests.TryRemove(requestId, out var ts))
                    ts.TrySetCanceled();
            }).ConfigureAwait(false);

            cts.Token.ThrowIfCancellationRequested();

            await _connection.WithWriteLockAsync(async (writer, arg) =>
            {
                var (subjectIn, bodyIn, replyToIn) = arg;

                if (_inboxSubscription == null)
                {
                    SetupInboxSubscription();

                    await SubCmd.WriteAsync(
                        writer,
                        _inboxSubscription.SubscriptionInfo.Subject.AsMemory(),
                        _inboxSubscription.SubscriptionInfo.Id.AsMemory(),
                        _inboxSubscription.SubscriptionInfo.QueueGroup.AsMemory()).ConfigureAwait(false);
                }

                await PubCmd.WriteAsync(writer, subjectIn, replyToIn, bodyIn).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }, Tuple.Create(subject.AsMemory(), body, replyToSubject.AsMemory())).ConfigureAwait(false);

            return await taskComp.Task.ConfigureAwait(false);
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

            var subscription = CreateSubscription(subscriptionInfo, subscriptionFactory);

            if (!IsConnected)
                return subscription;

            DoSub(subscriptionInfo);

            return subscription;
        }

        private void DoSub(SubscriptionInfo subscriptionInfo) => _connection.WithWriteLock((writer, arg) =>
        {
            var sid = arg.Id.AsSpan();

            SubCmd.Write(writer, arg.Subject, sid, arg.QueueGroup);

            if (arg.MaxMessages.HasValue)
                UnsubCmd.Write(writer, sid, arg.MaxMessages);

            writer.Flush();
        }, subscriptionInfo);

        public Task<ISubscription> SubAsync(string subject)
            => SubAsync(new SubscriptionInfo(subject));

        public Task<ISubscription> SubAsync(string subject, Func<INatsObservable<MsgOp>, IDisposable> subscriptionFactory)
            => SubAsync(new SubscriptionInfo(subject), subscriptionFactory);

        public Task<ISubscription> SubAsync(SubscriptionInfo subscriptionInfo)
            => SubAsync(subscriptionInfo, msgs => Disposable.Empty);

        public async Task<ISubscription> SubAsync(SubscriptionInfo subscriptionInfo, Func<INatsObservable<MsgOp>, IDisposable> subscriptionFactory)
        {
            ThrowIfDisposed();

            var subscription = CreateSubscription(subscriptionInfo, subscriptionFactory);

            if (!IsConnected)
                return subscription;

            await DoSubAsync(subscriptionInfo).ConfigureAwait(false);

            return subscription;
        }

        private Task DoSubAsync(SubscriptionInfo subscriptionInfo) =>
            _connection.WithWriteLockAsync(async (writer, arg) =>
            {
                var sid = arg.Id.AsMemory();

                await SubCmd.WriteAsync(writer, arg.Subject.AsMemory(), sid, arg.QueueGroup.AsMemory())
                    .ConfigureAwait(false);

                if (arg.MaxMessages.HasValue)
                    await UnsubCmd.WriteAsync(writer, sid, arg.MaxMessages).ConfigureAwait(false);

                await writer.FlushAsync().ConfigureAwait(false);
            }, subscriptionInfo);

        private Task DoSubAsync(SubscriptionInfo[] subscriptionInfos) =>
            _connection.WithWriteLockAsync(async (writer, arg) =>
            {
                foreach (var subscriptionInfo in arg)
                {
                    var sid = subscriptionInfo.Id.AsMemory();

                    await SubCmd.WriteAsync(writer, subscriptionInfo.Subject.AsMemory(), sid, subscriptionInfo.QueueGroup.AsMemory())
                        .ConfigureAwait(false);

                    if (subscriptionInfo.MaxMessages.HasValue)
                        await UnsubCmd.WriteAsync(writer, sid, subscriptionInfo.MaxMessages).ConfigureAwait(false);

                    await writer.FlushAsync().ConfigureAwait(false);
                }
            }, subscriptionInfos);

        private void DisposeSubscription(SubscriptionInfo subscriptionInfo)
        {
            if (_isDisposed)
                return;

            Swallow.Everything(() => Unsub(subscriptionInfo));
        }

        private Subscription CreateSubscription(SubscriptionInfo subscriptionInfo, Func<INatsObservable<MsgOp>, IDisposable> subscriptionFactory)
        {
            var subscription = Subscription.Create(
                subscriptionInfo,
                subscriptionFactory(MsgOpStream.Where(msg => msg.SubscriptionId == subscriptionInfo.Id)),
                DisposeSubscription);

            if (!_subscriptions.TryAdd(subscription.SubscriptionInfo.Id, subscription))
                throw NatsException.CouldNotCreateSubscription(subscription.SubscriptionInfo);

            return subscription;
        }

        public void Unsub(ISubscription subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            Unsub(subscription.SubscriptionInfo);
        }

        private bool TryRemoveSubscription(SubscriptionInfo subscriptionInfo)
        {
            _subscriptions.TryRemove(subscriptionInfo.Id, out var tmp);

            return tmp != null;
        }

        public void Unsub(SubscriptionInfo subscriptionInfo)
        {
            if (subscriptionInfo == null)
                throw new ArgumentNullException(nameof(subscriptionInfo));

            if (!TryRemoveSubscription(subscriptionInfo))
                return;

            if (!IsConnected)
                return;

            ThrowIfDisposed();

            _connection.WithWriteLock((writer, arg) =>
            {
                UnsubCmd.Write(writer, arg.Id, arg.MaxMessages);
                writer.Flush();
            }, subscriptionInfo);
        }

        public Task UnsubAsync(ISubscription subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            return UnsubAsync(subscription.SubscriptionInfo);
        }

        public async Task UnsubAsync(SubscriptionInfo subscriptionInfo)
        {
            if (subscriptionInfo == null)
                throw new ArgumentNullException(nameof(subscriptionInfo));

            if (!TryRemoveSubscription(subscriptionInfo))
                return;

            if (!IsConnected)
                return;

            ThrowIfDisposed();

            await _connection.WithWriteLockAsync(async (writer, arg) =>
            {
                await UnsubCmd.WriteAsync(writer, arg.Id.AsMemory(), arg.MaxMessages).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }, subscriptionInfo).ConfigureAwait(false);
        }
    }
}
