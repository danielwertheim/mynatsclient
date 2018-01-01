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

    /// <summary>
    /// Will not invoke <see cref="IObserver{T}.OnError"/> if an exception occurs.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SafeObservableOf<T> : Observable, IObservable<T>, IDisposable where T : class
    {
        private readonly ConcurrentDictionary<Guid, ObserverSubscription<T>> _subscriptions
            = new ConcurrentDictionary<Guid, ObserverSubscription<T>>();

        private bool _isDisposed;

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            GC.SuppressFinalize(this);

            var copy = _subscriptions
                .Values
                .OfType<IDisposable>()
                .ToArray();

            _subscriptions.Clear();

            Try.DisposeAll(copy);
        }

        public void Emit(T value)
        {
            ThrowIfDisposed();

            foreach (var subscription in _subscriptions.Values)
            {
                try
                {
                    subscription.OnNext(value);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error in observer while processing value.", ex);
                }
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            ThrowIfDisposed();

            var observerSubscription = ObserverSubscription<T>.Default(observer, OnDisposeSubscription);

            if (!_subscriptions.TryAdd(observerSubscription.Id, observerSubscription))
                throw new InvalidOperationException("Could not register observer.");

            return observerSubscription;
        }

        private void OnDisposeSubscription(ObserverSubscription<T> observerSubscription)
        {
            if (!_subscriptions.TryRemove(observerSubscription.Id, out observerSubscription))
                return;

            observerSubscription.OnCompleted();
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}