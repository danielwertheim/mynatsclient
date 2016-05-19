using System;

namespace NatsFun.Internals
{
    internal class SubscriptionOf<T> : IDisposable
    {
        public readonly Guid Id;
        private readonly IObserver<T> _observer;
        private readonly Action<SubscriptionOf<T>> _onDispose;

        internal SubscriptionOf(IObserver<T> observer, Action<SubscriptionOf<T>> onDispose)
        {
            Id = Guid.NewGuid();

            _observer = observer;
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose(this);
        }

        internal void OnNext(T value)
        {
            _observer.OnNext(value);
        }

        internal void OnError(Exception ex)
        {
            _observer.OnError(ex);
        }

        internal void OnCompleted()
        {
            _observer.OnCompleted();
        }
    }
}