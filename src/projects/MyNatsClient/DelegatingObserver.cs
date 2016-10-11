using System;
using EnsureThat;

namespace MyNatsClient
{
    /// <summary>
    /// Simple observer implementation that delgates
    /// to injected onNext, onError and onCompleted
    /// delegates.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DelegatingObserver<T> : IObserver<T>
    {
        private readonly Action<T> _onNext;
        private readonly Action<Exception> _onError;
        private readonly Action _onCompleted;

        public DelegatingObserver(
            Action<T> onNext,
            Action<Exception> onError = null,
            Action onCompleted = null)
        {
            EnsureArg.IsNotNull(onNext, nameof(onNext));

            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted; ;
        }

        public void OnNext(T value) => _onNext(value);

        public void OnCompleted() => _onCompleted?.Invoke();

        public void OnError(Exception error) => _onError?.Invoke(error);
    }
}