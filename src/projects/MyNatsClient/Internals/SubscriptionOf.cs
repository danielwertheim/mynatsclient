using System;

namespace MyNatsClient.Internals
{
    internal class SubscriptionOf<T> : IDisposable
    {
        public readonly Guid Id;
        private readonly IObserver<T> _observer;
        private readonly Action<SubscriptionOf<T>> _onDispose;
        private readonly Func<T, bool> _filter;
        private readonly bool _disposeOnError;
        private bool _isDisposed;

        private SubscriptionOf(
            IObserver<T> observer,
            Func<T, bool> filter,
            Action<SubscriptionOf<T>> onDispose,
            bool disposeOnError)
        {
            Id = Guid.NewGuid();

            _observer = observer;
            _filter = filter;
            _onDispose = onDispose;
            _disposeOnError = disposeOnError;
            _isDisposed = false;
        }

        private static bool ProcessAllFilter(T value) => true;

        internal static SubscriptionOf<T> Default(IObserver<T> observer, Action<SubscriptionOf<T>> onDispose, bool disposeOnError)
            => new SubscriptionOf<T>(observer, ProcessAllFilter, onDispose, disposeOnError);

        internal static SubscriptionOf<T> Filtered(IObserver<T> observer, Func<T, bool> filter, Action<SubscriptionOf<T>> onDispose, bool disposeOnError)
            => new SubscriptionOf<T>(observer, filter, onDispose, disposeOnError);

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
        {
            _observer.OnError(ex);

            if (_disposeOnError)
                Dispose();
        }

        internal void OnCompleted()
            => _observer.OnCompleted();
    }
}