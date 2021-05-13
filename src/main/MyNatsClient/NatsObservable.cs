using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using MyNatsClient.Internals;

namespace MyNatsClient
{
    public abstract class NatsObservable
    {
        protected readonly ILogger Logger;

        protected NatsObservable()
        {
            Logger = LoggerManager.CreateLogger(GetType());
        }
    }

    public sealed class NatsObservableOf<T> : NatsObservable, INatsObservable<T> where T : class
    {
        private readonly ConcurrentDictionary<int, ObSubscription> _subscriptions
            = new ConcurrentDictionary<int, ObSubscription>();

        private bool _isDisposed;

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            var exs = new List<Exception>();
            foreach (var s in _subscriptions.Values)
            {
                try
                {
                    s.Dispose();
                }
                catch (Exception e)
                {
                    exs.Add(e);
                }
            }

            _subscriptions.Clear();

            if (exs.Any())
                throw new AggregateException("Failed while disposing observable. See inner exception(s) for more details.", exs);
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        public void Emit(T value)
        {
            ThrowIfDisposed();

            foreach (var subscription in _subscriptions.Values)
            {
                try
                {
                    subscription.Observer.OnNext(value);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error in observer while emitting value. Observer subscription will be removed.");

                    //No invoke of OnError as it's not the producer that is failing.
                    _subscriptions.TryRemove(subscription.Id, out _);

                    Swallow.Everything(() => subscription.Dispose());
                }
            }
        }

        private void DisposeSubscription(ObSubscription sub)
        {
            if (!_subscriptions.TryRemove(sub.Id, out _))
                return; //Most likely, previously removed due to failure

            Swallow.Everything(() => sub.Observer.OnCompleted());
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            ThrowIfDisposed();

            if (observer == null)
                throw new ArgumentNullException(nameof(observer));

            var sub = new ObSubscription(observer, DisposeSubscription);

            if (!_subscriptions.TryAdd(sub.Id, sub))
                throw new ArgumentException("Can not subscribe observer. Each observer can only be subscribed once.", nameof(observer));

            return sub;
        }

        private sealed class ObSubscription : IDisposable
        {
            private readonly Action<ObSubscription> _onDispose;

            public readonly int Id;
            public readonly IObserver<T> Observer;

            public ObSubscription(IObserver<T> observer, Action<ObSubscription> onDispose)
            {
                Id = observer.GetHashCode();
                Observer = observer;
                _onDispose = onDispose;
            }

            public void Dispose() => _onDispose(this);
        }
    }
}
