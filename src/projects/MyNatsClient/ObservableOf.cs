using System;
using System.Collections.Concurrent;
using System.Linq;
using MyNatsClient.Internals;

namespace MyNatsClient
{
    public abstract class Observable
    {
        protected readonly ILogger Logger;

        protected Observable()
        {
            Logger = LoggerManager.Resolve(typeof(Observable));
        }
    }

    public class ObservableOf<T> : Observable, INatsObservable<T>, IDisposable where T : class
    {
        private readonly ConcurrentDictionary<Guid, ObserverSubscription<T>> _subscriptions = new ConcurrentDictionary<Guid, ObserverSubscription<T>>();
        private bool _isDisposed;

        public Action<T, Exception> OnException { private get; set; }

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
                    Logger.Error("Error in observer while processing message.", ex);

                    OnException?.Invoke(ev, ex);

                    subscription.OnError(ex);
                }
            }
        }

        public virtual IDisposable Subscribe(IObserver<T> observer)
        {
            ThrowIfDisposed();

            return Subscribe(ObserverSubscription<T>.Default(observer, OnDisposeSubscription));
        }

        public virtual IDisposable Subscribe(IObserver<T> observer, Func<T, bool> filter)
        {
            ThrowIfDisposed();

            return Subscribe(ObserverSubscription<T>.Filtered(observer, filter, OnDisposeSubscription));
        }

        private IDisposable Subscribe(ObserverSubscription<T> observerSubscription)
        {
            if (_subscriptions.TryAdd(observerSubscription.Id, observerSubscription))
                return observerSubscription;

            throw new InvalidOperationException("Could not register observer.");
        }

        private void OnDisposeSubscription(ObserverSubscription<T> observerSubscription)
        {
            if (_subscriptions.TryRemove(observerSubscription.Id, out observerSubscription))
                observerSubscription.OnCompleted();
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}