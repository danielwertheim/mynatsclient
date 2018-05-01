using System;

namespace MyNatsClient.Internals
{
    internal class DelegatingObserver<T> : IObserver<T>
    {
        private readonly Action<T> _onNext;
        private readonly Action<Exception> _onError;
        private readonly Action _onCompleted;

        internal DelegatingObserver(
            Action<T> onNext,
            Action<Exception> onError = null,
            Action onCompleted = null)
        {
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
        }

        public void OnNext(T value)
            => _onNext(value);

        public void OnError(Exception error)
            => _onError?.Invoke(error);

        public void OnCompleted()
            => _onCompleted?.Invoke();
    }
}