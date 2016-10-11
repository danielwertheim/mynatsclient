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
    public class NatsExchange : IDisposable, INatsExchange
    {
        private bool _isDisposed;
        private NatsClient _client;
        private readonly ConcurrentDictionary<string, ISubscription> _subscriptions;

        public INatsClient Client => _client;

        public NatsExchange(string id, ConnectionInfo connectionInfo)
        {
            _client = new NatsClient(id, connectionInfo);
            _client.Events.Subscribe(new DelegatingObserver<IClientEvent>(OnClientEvent));

            _subscriptions = new ConcurrentDictionary<string, ISubscription>();
        }

        private static void OnClientEvent(IClientEvent ev)
        {
            var disconnected = ev as ClientDisconnected;
            if (disconnected == null)
                return;

            OnClientDisconnected(disconnected);
        }

        private static void OnClientDisconnected(ClientDisconnected disconnected)
        {
            if (disconnected.Reason != DisconnectReason.DueToFailure)
                return;

            disconnected.Client.Connect();
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

            var subscriptions = _subscriptions.Values.ToArray();
            _subscriptions.Clear();

            Try.All(
                () => Try.DisposeAll(subscriptions),
                () =>
                {
                    _client?.Disconnect();
                    _client?.Dispose();
                    _client = null;
                });
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        public void Connect() => Client.Connect();

        public void Disconnect() => Client.Disconnect();

        public ISubscription Subscribe(string subject, IObserver<MsgOp> observer, int? unsubAfterNMessages = null)
            => Subscribe(new SubscriptionInfo(subject), observer, unsubAfterNMessages);

        public ISubscription Subscribe(SubscriptionInfo subscriptionInfo, IObserver<MsgOp> observer, int? unsubAfterNMessages = null)
        {
            ThrowIfDisposed();

            var subscription = CreateSubscription(subscriptionInfo, observer);

            Client.Sub(subscription.SubscriptionInfo);
            if (unsubAfterNMessages.HasValue)
                Client.UnSub(subscription.SubscriptionInfo, unsubAfterNMessages);

            return subscription;
        }

        public Task<ISubscription> SubscribeAsync(string subject, IObserver<MsgOp> observer, int? unsubAfterNMessages = null)
            => SubscribeAsync(new SubscriptionInfo(subject), observer, unsubAfterNMessages);

        public async Task<ISubscription> SubscribeAsync(SubscriptionInfo subscriptionInfo, IObserver<MsgOp> observer, int? unsubAfterNMessages = null)
        {
            ThrowIfDisposed();

            var subscription = CreateSubscription(subscriptionInfo, observer);

            await Client.SubAsync(subscription.SubscriptionInfo).ForAwait();
            if (unsubAfterNMessages.HasValue)
                await Client.UnSubAsync(subscription.SubscriptionInfo, unsubAfterNMessages).ForAwait();

            return subscription;
        }

        private Subscription CreateSubscription(SubscriptionInfo subscriptionInfo, IObserver<MsgOp> observer)
        {
            var subscription = new Subscription(subscriptionInfo, Client.MsgOpStream, observer, info =>
            {
                ISubscription tmp;
                _subscriptions.TryRemove(info.Id, out tmp);
            });
            if (!_subscriptions.TryAdd(subscription.SubscriptionInfo.Id, subscription))
                throw new NatsException($"Could not create subscription. There is already a subscription with the id '{subscription.SubscriptionInfo.Id}'; registrered.");

            return subscription;
        }
    }
}