using System;

namespace MyNatsClient.Internals
{
    internal class SubscriptionOf<T> : IDisposable
    {
        public readonly Guid Id;
        private readonly IObserver<T> _observer;
        private readonly Action<SubscriptionOf<T>> _onDispose;
        private readonly Func<T, bool> _filter;

        private SubscriptionOf(
            IObserver<T> observer,
            Func<T, bool> filter,
            Action<SubscriptionOf<T>> onDispose)
        {
            Id = Guid.NewGuid();

            _observer = observer;
            _filter = filter;
            _onDispose = onDispose;
        }

        private static bool ProcessAllFilter(T value) => true;

        internal static SubscriptionOf<T> Default(IObserver<T> observer, Action<SubscriptionOf<T>> onDispose)
            => new SubscriptionOf<T>(observer, ProcessAllFilter, onDispose);

        internal static SubscriptionOf<T> Filtered(IObserver<T> observer, Func<T, bool> filter, Action<SubscriptionOf<T>> onDispose)
            => new SubscriptionOf<T>(observer, filter, onDispose);

        public void Dispose()
        {
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
            Dispose();
        }

        internal void OnCompleted()
        {
            _observer.OnCompleted();
        }
    }
}