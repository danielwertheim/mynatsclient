using System;

namespace MyNatsClient.Internals
{
    internal class ObserverSubscription<T> : IDisposable
    {
        public readonly Guid Id;
        private readonly IObserver<T> _observer;
        private readonly Action<ObserverSubscription<T>> _onDispose;
        private readonly Func<T, bool> _filter;
        private bool _isDisposed;

        private ObserverSubscription(
            IObserver<T> observer,
            Func<T, bool> filter,
            Action<ObserverSubscription<T>> onDispose)
        {
            Id = Guid.NewGuid();

            _observer = observer;
            _filter = filter;
            _onDispose = onDispose;
            _isDisposed = false;
        }

        private static bool ProcessAllFilter(T value) => true;

        internal static ObserverSubscription<T> Default(IObserver<T> observer, Action<ObserverSubscription<T>> onDispose)
            => new ObserverSubscription<T>(observer, ProcessAllFilter, onDispose);

        internal static ObserverSubscription<T> Filtered(IObserver<T> observer, Func<T, bool> filter, Action<ObserverSubscription<T>> onDispose)
            => new ObserverSubscription<T>(observer, filter, onDispose);

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
            if (_filter(value))
                _observer.OnNext(value);
        }

        internal void OnError(Exception ex)
            => _observer.OnError(ex);

        internal void OnCompleted()
            => _observer.OnCompleted();
    }
}