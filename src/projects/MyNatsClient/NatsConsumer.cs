using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using MyNatsClient.Events;
using MyNatsClient.Internals;
using MyNatsClient.Internals.Extensions;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public class NatsConsumer : IDisposable, INatsConsumer
    {
        private bool _isDisposed;
        private readonly INatsClient _client;
        private readonly ConcurrentDictionary<string, IConsumerSubscription> _subscriptions;

        public NatsConsumer(INatsClient client)
        {
            _client = client;
            _subscriptions = new ConcurrentDictionary<string, IConsumerSubscription>();
            _client.Events.Subscribe(new DelegatingObserver<IClientEvent>(OnClientEvent));
        }

        private void OnClientEvent(IClientEvent ev)
        {
            var connected = ev as ClientConnected;
            if (connected == null)
                return;

            OnClientConnected(connected);
        }

        private void OnClientConnected(ClientConnected ev)
        {
            foreach (var subscription in _subscriptions.Values)
                _client.Sub(subscription.SubscriptionInfo);
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

            var subscriptions = _subscriptions.Values.Cast<IDisposable>().ToArray();
            if (!subscriptions.Any())
                return;

            Try.DisposeAll(subscriptions);

            _subscriptions.Clear();
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        public IConsumerSubscription Subscribe(string subject, IObserver<MsgOp> observer, int? unsubAfterNMessages = null)
            => Subscribe(new SubscriptionInfo(subject), observer, unsubAfterNMessages);

        public IConsumerSubscription Subscribe(SubscriptionInfo subscriptionInfo, IObserver<MsgOp> observer, int? unsubAfterNMessages = null)
        {
            ThrowIfDisposed();

            var subscription = CreateSubscription(subscriptionInfo, observer);

            if (_client.State != NatsClientState.Connected)
                return subscription;

            _client.Sub(subscription.SubscriptionInfo);

            if (unsubAfterNMessages.HasValue)
                _client.UnSub(subscription.SubscriptionInfo, unsubAfterNMessages);

            return subscription;
        }

        public Task<IConsumerSubscription> SubscribeAsync(string subject, IObserver<MsgOp> observer, int? unsubAfterNMessages = null)
            => SubscribeAsync(new SubscriptionInfo(subject), observer, unsubAfterNMessages);

        public async Task<IConsumerSubscription> SubscribeAsync(SubscriptionInfo subscriptionInfo, IObserver<MsgOp> observer, int? unsubAfterNMessages = null)
        {
            ThrowIfDisposed();

            var subscription = CreateSubscription(subscriptionInfo, observer);

            if (_client.State != NatsClientState.Connected)
                return subscription;

            await _client.SubAsync(subscription.SubscriptionInfo).ForAwait();

            if (unsubAfterNMessages.HasValue)
                await _client.UnSubAsync(subscription.SubscriptionInfo, unsubAfterNMessages).ForAwait();

            return subscription;
        }

        private ConsumerSubscription CreateSubscription(SubscriptionInfo subscriptionInfo, IObserver<MsgOp> observer)
        {
            var subscription = new ConsumerSubscription(subscriptionInfo, _client.MsgOpStream, observer, info =>
            {
                IConsumerSubscription tmp;
                _subscriptions.TryRemove(info.Id, out tmp);
                if (_client.State == NatsClientState.Connected && tmp != null)
                    Swallow.Everything(() => _client.UnSub(tmp.SubscriptionInfo));
            });
            if (!_subscriptions.TryAdd(subscription.SubscriptionInfo.Id, subscription))
                throw new NatsException($"Could not create subscription. Id='{subscriptionInfo.Id}'. Subject='{subscriptionInfo.Subject}' QueueGroup='{subscriptionInfo.QueueGroup}'; registrered.");

            return subscription;
        }
    }
}