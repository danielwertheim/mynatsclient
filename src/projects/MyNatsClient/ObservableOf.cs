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
        private readonly ConcurrentDictionary<Guid, ObserverSubscription<T>> _subscriptions
            = new ConcurrentDictionary<Guid, ObserverSubscription<T>>();

        private bool _isDisposed;
        private bool _hasFailed;

        public Action<T, Exception> OnException { private get; set; }

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

            //ThrowIfFailed();

            foreach (var subscription in _subscriptions.Values)
                NotifyObserver(value, subscription);
        }

        private void NotifyObserver(T value, ObserverSubscription<T> subscription)
        {
            try
            {
                subscription.OnNext(value);
            }
            catch (Exception ex)
            {
                _hasFailed = true;

                OnException?.Invoke(value, ex);

                var aggEx = new Exception("An exception occured while notifying observers of a new value.", ex);

                //foreach (var s in _subscriptions.Values)
                //{
                    Logger.Error("Error in observer while processing message.", ex);

                Swallow.Everything(
                    () => subscription.OnError(ex),
                    () => subscription.Dispose());

                    //Swallow.Everything(
                    //    () => s.OnError(ex),
                    //    () => s.Dispose());
                //}

                //throw aggEx;
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            ThrowIfDisposed();

            ThrowIfFailed();

            var observerSubscription = ObserverSubscription<T>.Default(observer, OnDisposeSubscription);

            if (!_subscriptions.TryAdd(observerSubscription.Id, observerSubscription))
                throw new InvalidOperationException("Could not register observer.");

            return observerSubscription;
        }

        private void OnDisposeSubscription(ObserverSubscription<T> observerSubscription)
        {
            if (!_subscriptions.TryRemove(observerSubscription.Id, out observerSubscription))
                return;

            if (!_hasFailed)
                observerSubscription.OnCompleted();
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        private void ThrowIfFailed()
        {
            if (_hasFailed)
                throw new InvalidOperationException("The observable is marked as failed.");
        }
    }
}