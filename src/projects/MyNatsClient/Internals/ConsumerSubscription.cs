using System;
using EnsureThat;
using MyNatsClient.Ops;

namespace MyNatsClient.Internals
{
    internal class ConsumerSubscription : IConsumerSubscription
    {
        private IDisposable _subscription;
        private bool _isDisposed;
        private Action<SubscriptionInfo> _onDisposing;

        public SubscriptionInfo SubscriptionInfo { get; }

        internal ConsumerSubscription(
            SubscriptionInfo subscriptionInfo,
            IFilterableObservable<MsgOp> messageStream,
            IObserver<MsgOp> observer,
            Action<SubscriptionInfo> onDisposing = null)
        {
            EnsureArg.IsNotNull(subscriptionInfo, nameof(subscriptionInfo));
            EnsureArg.IsNotNull(messageStream, nameof(messageStream));
            EnsureArg.IsNotNull(observer, nameof(observer));

            SubscriptionInfo = subscriptionInfo;
            _subscription = messageStream.Subscribe(observer, msg => SubscriptionInfo.Matches(msg.Subject));
            _onDisposing = onDisposing;
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