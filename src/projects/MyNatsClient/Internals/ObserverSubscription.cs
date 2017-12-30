using System;

namespace MyNatsClient.Internals
{
    internal class ObserverSubscription<T> : IDisposable
    {
        public readonly Guid Id;
        private readonly IObserver<T> _observer;
        private readonly Action<ObserverSubscription<T>> _onDispose;
        private bool _isDisposed;

        private ObserverSubscription(
            IObserver<T> observer,
            Action<ObserverSubscription<T>> onDispose)
        {
            Id = Guid.NewGuid();

            _observer = observer;
            _onDispose = onDispose;
            _isDisposed = false;
        }

        internal static ObserverSubscription<T> Default(IObserver<T> observer, Action<ObserverSubscription<T>> onDispose)
            => new ObserverSubscription<T>(observer, onDispose);

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            GC.SuppressFinalize(this);

            _onDispose(this);
        }

        internal void OnNext(T value)
        {
            _observer.OnNext(value);
        }

        internal void OnError(Exception ex)
            => _observer.OnError(ex);

        internal void OnCompleted()
            => _observer.OnCompleted();
    }
}