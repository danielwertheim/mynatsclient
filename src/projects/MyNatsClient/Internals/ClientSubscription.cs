using System;
using EnsureThat;

namespace MyNatsClient.Internals
{
    internal class ClientSubscription : IClientSubscription
    {
        private IDisposable _subscription;
        private bool _isDisposed;
        private Action<SubscriptionInfo> _onDisposing;

        public SubscriptionInfo SubscriptionInfo { get; }

        private ClientSubscription(
            SubscriptionInfo subscriptionInfo,
            IDisposable subscription,
            Action<SubscriptionInfo> onDisposing)
        {
            EnsureArg.IsNotNull(subscriptionInfo, nameof(subscriptionInfo));
            EnsureArg.IsNotNull(subscription, nameof(subscription));
            EnsureArg.IsNotNull(onDisposing, nameof(onDisposing));

            SubscriptionInfo = subscriptionInfo;
            _subscription = subscription;
            _onDisposing = onDisposing;
        }

        internal static ClientSubscription Create(
            SubscriptionInfo subscriptionInfo,
            IDisposable subscription,
            Action<SubscriptionInfo> onDisposing)
        {
            return new ClientSubscription(
                subscriptionInfo,
                subscription,
                onDisposing);
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

            Try.All(
                () =>
                {
                    Try.DisposeAll(_subscription);
                    _subscription = null;
                },
                () =>
                {
                    _onDisposing?.Invoke(SubscriptionInfo);
                    _onDisposing = null;
                });
        }
    }
}