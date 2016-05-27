using System;
using System.Collections.Concurrent;
using System.Linq;
using MyNatsClient.Internals;

namespace MyNatsClient
{
    public class ObservableOf<T> : IObservable<T>, IDisposable
    {
        private readonly ConcurrentDictionary<Guid, SubscriptionOf<T>> _subscriptions = new ConcurrentDictionary<Guid, SubscriptionOf<T>>();
        private bool _isDisposed;

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

            var subscription = new SubscriptionOf<T>(observer, s =>
            {
                if (_subscriptions.TryRemove(s.Id, out s))
                    s.OnCompleted();
            });

            if (_subscriptions.TryAdd(subscription.Id, subscription))
                return subscription;

            throw new InvalidOperationException("Could not register observer.");
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

            Try.DisposeAll(_subscriptions.Values.ToArray());
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}