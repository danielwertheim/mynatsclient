using System;

namespace MyNatsClient.Internals
{
    internal sealed class OfTypeObservable<TResult> : NatsObservableOf<TResult> where TResult : class
    {
        private readonly IObservable<object> _src;

        public OfTypeObservable(IObservable<object> src)
        {
            _src = src;
        }

        public override IDisposable Subscribe(IObserver<TResult> observer)
            => _src.Subscribe(new OfTypeObserver(observer));

        private sealed class OfTypeObserver : IObserver<object>
        {
            private readonly IObserver<TResult> _observer;

            public OfTypeObserver(IObserver<TResult> observer)
            {
                _observer = observer;
            }

            public void OnNext(object value)
            {
                if (value is TResult result)
                    _observer.OnNext(result);
            }

            public void OnError(Exception error)
                => _observer.OnError(error);

            public void OnCompleted()
                => _observer.OnCompleted();
        }
    }

    internal sealed class CastObservable<TFrom, TTo> : NatsObservableOf<TTo> where TFrom : class where TTo : class
    {
        private readonly IObservable<TFrom> _src;

        public CastObservable(IObservable<TFrom> src)
        {
            _src = src;
        }

        public override IDisposable Subscribe(IObserver<TTo> observer)
            => _src.Subscribe(new CastObserver(observer));

        private sealed class CastObserver : IObserver<TFrom>
        {
            private readonly IObserver<TTo> _observer;

            public CastObserver(IObserver<TTo> observer)
            {
                _observer = observer;
            }

            public void OnNext(TFrom value)
            {
                _observer.OnNext(value as TTo);
            }

            public void OnError(Exception error)
                => _observer.OnError(error);

            public void OnCompleted()
                => _observer.OnCompleted();
        }
    }

    internal sealed class WhereObservable<T> : NatsObservableOf<T> where T : class
    {
        private readonly IObservable<T> _src;
        private readonly Func<T, bool> _predicate;

        public WhereObservable(IObservable<T> src, Func<T, bool> predicate)
        {
            _src = src;
            _predicate = predicate;
        }

        public override IDisposable Subscribe(IObserver<T> observer)
            => _src.Subscribe(new WhereObserver(observer, _predicate));

        private sealed class WhereObserver : IObserver<T>
        {
            private readonly IObserver<T> _observer;
            private readonly Func<T, bool> _predicate;

            public WhereObserver(IObserver<T> observer, Func<T, bool> predicate)
            {
                _observer = observer;
                _predicate = predicate;
            }

            public void OnNext(T value)
            {
                if (_predicate(value))
                    _observer.OnNext(value);
            }

            public void OnError(Exception error)
                => _observer.OnError(error);

            public void OnCompleted()
                => _observer.OnCompleted();
        }
    }
}