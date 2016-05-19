using System;
using System.Collections.Concurrent;
using System.Linq;
using NatsFun.Internals;

namespace NatsFun
{
    public class NatsOpMediator : IObservable<IOp>, INatsClientStats, IDisposable
    {
        private readonly ConcurrentDictionary<Guid, SubscriptionOf<IOp>> _subscriptions = new ConcurrentDictionary<Guid, SubscriptionOf<IOp>>();
        private bool _isDisposed;

        public DateTime LastOpReceivedAt { get; private set; }
        public long OpCount { get; private set; }

        public void Dispatch(IOp op)
        {
            LastOpReceivedAt = DateTime.UtcNow;
            OpCount++;

            foreach (var subscription in _subscriptions.Values)
            {
                try
                {
                    subscription.OnNext(op);
                }
                catch (Exception ex)
                {
                    subscription.OnError(ex);
                }
            }
        }

        public IDisposable Subscribe(IObserver<IOp> observer)
        {
            ThrowIfDisposed();

            var subscription = new SubscriptionOf<IOp>(observer, s =>
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