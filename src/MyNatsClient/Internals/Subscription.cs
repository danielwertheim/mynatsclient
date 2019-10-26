using System;

namespace MyNatsClient.Internals
{
    internal sealed class Subscription : ISubscription
    {
        private const string DisposeExMessage = "Failed while disposing subscription. See inner exception(s) for more details.";

        private IDisposable _subscription;
        private bool _isDisposed;
        private Action<SubscriptionInfo> _onDisposing;

        public SubscriptionInfo SubscriptionInfo { get; }

        private Subscription(
            SubscriptionInfo subscriptionInfo,
            IDisposable subscription,
            Action<SubscriptionInfo> onDisposing)
        {
            SubscriptionInfo = subscriptionInfo ?? throw new ArgumentNullException(nameof(subscriptionInfo));
            _subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
            _onDisposing = onDisposing ?? throw new ArgumentNullException(nameof(onDisposing));
        }

        internal static Subscription Create(
            SubscriptionInfo subscriptionInfo,
            IDisposable subscription,
            Action<SubscriptionInfo> onDisposing)
        {
            return new Subscription(
                subscriptionInfo,
                subscription,
                onDisposing);
        }

        public void Dispose()
        {
            if(_isDisposed)
                return;

            _isDisposed = true;

            Exception ex1 = null, ex2 = null;

            try
            {
                _subscription.Dispose();
            }
            catch (Exception ex)
            {
                ex1 = ex;
            }

            try
            {
                _onDisposing.Invoke(SubscriptionInfo);
            }
            catch (Exception ex)
            {
                ex2 = ex;
            }

            _subscription = null;
            _onDisposing = null;

            if(ex1 == null && ex2 == null)
                return;

            if(ex1 != null && ex2 != null)
                throw new AggregateException(DisposeExMessage, ex1, ex2);
            
            if(ex1 != null)
                throw new AggregateException(DisposeExMessage, ex1);
            
            if(ex2 != null)
                throw new AggregateException(DisposeExMessage, ex2);
        }
    }
}