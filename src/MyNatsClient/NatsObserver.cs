using System;
using MyNatsClient.Internals;

namespace MyNatsClient
{
    public static class NatsObserver
    {
        public static IObserver<T> Delegating<T>(
            Action<T> onNext,
            Action<Exception> onError = null,
            Action onCompleted = null) => new DelegatingObserver<T>(onNext, onError, onCompleted);

        public static IObserver<T> Safe<T>(
            Action<T> onNext,
            Action<Exception> onError = null,
            Action onCompleted = null) => new SafeObserver<T>(onNext, onError, onCompleted);
    }
}