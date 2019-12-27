using System;
using MyNatsClient.Ops;
using MyNatsClient.Rx;

namespace MyNatsClient.Internals
{
    internal sealed class OfTypeObservable<TResult> : INatsObservable<TResult> where TResult : class
    {
        private readonly INatsObservable<object> _src;

        public OfTypeObservable(INatsObservable<object> src) 
            => _src = src ?? throw new ArgumentNullException(nameof(src));

        public void Dispose() => _src.Dispose();

        public IDisposable Subscribe(IObserver<TResult> observer)
            => _src.SubscribeSafe(new OfTypeObserver(observer));

        private sealed class OfTypeObserver : IObserver<object>
        {
            private readonly IObserver<TResult> _observer;

            public OfTypeObserver(IObserver<TResult> observer)
                => _observer = observer;

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

    internal sealed class CastObservable<TFrom, TTo> : INatsObservable<TTo> where TFrom : class where TTo : class
    {
        private readonly INatsObservable<TFrom> _src;

        public CastObservable(INatsObservable<TFrom> src) 
            => _src = src ?? throw new ArgumentNullException(nameof(src));

        public void Dispose() => _src.Dispose();

        public IDisposable Subscribe(IObserver<TTo> observer)
            => _src.SubscribeSafe(new CastObserver(observer));

        private sealed class CastObserver : IObserver<TFrom>
        {
            private readonly IObserver<TTo> _observer;

            public CastObserver(IObserver<TTo> observer) 
                => _observer = observer;

            public void OnNext(TFrom value)
                => _observer.OnNext(value as TTo);

            public void OnError(Exception error)
                => _observer.OnError(error);

            public void OnCompleted()
                => _observer.OnCompleted();
        }
    }

    internal sealed class WhereObservable<T> : INatsObservable<T> where T : class
    {
        private readonly INatsObservable<T> _src;
        private readonly Func<T, bool> _predicate;

        public WhereObservable(INatsObservable<T> src, Func<T, bool> predicate)
        {
            _src = src ?? throw new ArgumentNullException(nameof(src));
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        public void Dispose() => _src.Dispose();

        public IDisposable Subscribe(IObserver<T> observer)
            => _src.SubscribeSafe(new WhereObserver(observer, _predicate));

        private sealed class WhereObserver : IObserver<T>
        {
            private readonly IObserver<T> _observer;
            private readonly Func<T, bool> _predicate;

            public WhereObserver(IObserver<T> observer, Func<T, bool> predicate)
            {
                _observer = observer ?? throw new ArgumentNullException(nameof(observer));
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

    internal sealed class WhereSubjectMatchObservable : INatsObservable<MsgOp>
    {
        private readonly INatsObservable<MsgOp> _src;
        private readonly string _subject;

        public WhereSubjectMatchObservable(INatsObservable<MsgOp> src, string subject)
        {
            _src = src ?? throw new ArgumentNullException(nameof(src));
            _subject = subject ?? throw new ArgumentNullException(nameof(subject));
        }

        public void Dispose() => _src.Dispose();

        public IDisposable Subscribe(IObserver<MsgOp> observer)
            => _src.SubscribeSafe(new WhereSubjectMatchObserver(observer, _subject));

        private sealed class WhereSubjectMatchObserver : IObserver<MsgOp>
        {
            private readonly IObserver<MsgOp> _observer;
            private readonly string _subject;

            public WhereSubjectMatchObserver(IObserver<MsgOp> observer, string subject)
            {
                _observer = observer ?? throw new ArgumentNullException(nameof(observer));
                _subject = subject;
            }

            public void OnNext(MsgOp value)
            {
                //TODO: Wildcards etc
                if (_subject.Equals(value.Subject, StringComparison.Ordinal))
                    _observer.OnNext(value);
            }

            public void OnError(Exception error)
                => _observer.OnError(error);

            public void OnCompleted()
                => _observer.OnCompleted();
        }
    }

    internal sealed class SelectObservable<TFrom, TTo> : INatsObservable<TTo> where TFrom : class where TTo : class
    {
        private readonly INatsObservable<TFrom> _src;
        private readonly Func<TFrom, TTo> _map;

        public SelectObservable(INatsObservable<TFrom> src, Func<TFrom, TTo> map)
        {
            _src = src ?? throw new ArgumentNullException(nameof(src));
            _map = map ?? throw new ArgumentNullException(nameof(src));
        }

        public void Dispose() => _src.Dispose();

        public IDisposable Subscribe(IObserver<TTo> observer)
            => _src.SubscribeSafe(new SelectObserver(observer, _map));

        private sealed class SelectObserver : IObserver<TFrom>
        {
            private readonly IObserver<TTo> _observer;
            private readonly Func<TFrom, TTo> _map;

            public SelectObserver(IObserver<TTo> observer, Func<TFrom, TTo> map)
            {
                _observer = observer ?? throw new ArgumentNullException(nameof(observer));
                _map = map;
            }

            public void OnNext(TFrom value)
                => _observer.OnNext(_map(value));

            public void OnError(Exception error)
                => _observer.OnError(error);

            public void OnCompleted()
                => _observer.OnCompleted();
        }
    }
}