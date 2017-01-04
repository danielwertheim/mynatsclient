using System;
using System.Collections.Concurrent;
using System.Linq;
using MyNatsClient.Internals;

namespace MyNatsClient
{
    public class ObservableOf<T> : IFilterableObservable<T>, IDisposable
    {
        private readonly ConcurrentDictionary<Guid, SubscriptionOf<T>> _subscriptions = new ConcurrentDictionary<Guid, SubscriptionOf<T>>();
        private readonly bool _autoRemoveFailingSubscription;
        private bool _isDisposed;

        public ObservableOf(bool autoRemoveFailingSubscription)
        {
            _autoRemoveFailingSubscription = autoRemoveFailingSubscription;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            GC.SuppressFinalize(this);

            var copy = _subscriptions.Values.ToArray();
            _subscriptions.Clear();

            Try.DisposeAll(copy);
        }

        public virtual void Dispatch(T ev)
        {
            foreach (var subscription in _subscriptions.Values)
            {
                try
                {
                    subscription.OnNext(ev);
                }
                catch (Exception ex)
                {
                    subscription.OnError(ex);
                }
            }
        }

        public virtual IDisposable Subscribe(IObserver<T> observer)
        {
            ThrowIfDisposed();

            return Subscribe(SubscriptionOf<T>.Default(observer, OnDisposeSubscription, _autoRemoveFailingSubscription));
        }

        public virtual IDisposable Subscribe(IObserver<T> observer, Func<T, bool> filter)
        {
            ThrowIfDisposed();

            return Subscribe(SubscriptionOf<T>.Filtered(observer, filter, OnDisposeSubscription, _autoRemoveFailingSubscription));
        }

        private IDisposable Subscribe(SubscriptionOf<T> subscription)
        {
            if (_subscriptions.TryAdd(subscription.Id, subscription))
                return subscription;

            throw new InvalidOperationException("Could not register observer.");
        }

        private void OnDisposeSubscription(SubscriptionOf<T> subscription)
        {
            if (_subscriptions.TryRemove(subscription.Id, out subscription))
                subscription.OnCompleted();
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}