using System;
using MyNatsClient.Internals;
using MyNatsClient.Ops;

namespace MyNatsClient.Rx
{
    public static class NatsObservableExtensions
    {
        public static INatsObservable<T> Catch<TException, T>(this INatsObservable<T> ob, Action<TException> handler)
            where TException : Exception
            where T : class
            => new CatchObservable<T, TException>(ob, handler);

        public static INatsObservable<T> CatchAny<T>(this INatsObservable<T> ob, Action<Exception> handler)
            where T : class
            => ob.Catch(handler);

        public static INatsObservable<TResult> OfType<TResult>(this INatsObservable<object> ob) where TResult : class
            => new OfTypeObservable<TResult>(ob);

        public static INatsObservable<TResult> Cast<TSource, TResult>(this INatsObservable<TSource> ob) where TSource : class where TResult : class
            => new CastObservable<TSource, TResult>(ob);

        public static INatsObservable<TResult> Select<TSource, TResult>(this INatsObservable<TSource> ob, Func<TSource, TResult> map) where TSource : class where TResult : class
            => new SelectObservable<TSource, TResult>(ob, map);

        public static INatsObservable<T> Where<T>(this INatsObservable<T> ob, Func<T, bool> predicate) where T : class
            => new WhereObservable<T>(ob, predicate);

        public static INatsObservable<MsgOp> WhereSubjectMatches(this INatsObservable<MsgOp> ob, string subject)
            => new WhereSubjectMatchObservable(ob, subject);

        public static IDisposable Subscribe<T>(this INatsObservable<T> ob, Action<T> onNext, Action<Exception> onError = null, Action onCompleted = null)
            => ob.Subscribe(NatsObserver.Delegating(onNext, onError, onCompleted));

        public static IDisposable SubscribeSafe<T>(this INatsObservable<T> ob, Action<T> onNext, Action<Exception> onError = null, Action onCompleted = null)
            => ob.Subscribe(NatsObserver.Safe(onNext, onError, onCompleted));

        public static IDisposable SubscribeSafe<T>(this INatsObservable<T> ob, IObserver<T> observer) where T : class
            => ob.Subscribe(NatsObserver.Safe<T>(observer.OnNext, observer.OnError, observer.OnCompleted));
    }
}
