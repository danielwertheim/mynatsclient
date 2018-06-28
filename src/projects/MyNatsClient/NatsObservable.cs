using System;
using System.Collections.Concurrent;
using MyNatsClient.Internals;

namespace MyNatsClient
{
    public abstract class NatsObservable
    {
        protected readonly ILogger Logger;

        protected NatsObservable()
        {
            Logger = LoggerManager.Resolve(typeof(NatsObservable));
        }
    }

    public class NatsObservableOf<T> : NatsObservable, INatsObservable<T>, IDisposable where T : class
    {
        private readonly ConcurrentDictionary<Guid, ObSubscription> _subscriptions
            = new ConcurrentDictionary<Guid, ObSubscription>();

        private bool _isDisposed;

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            GC.SuppressFinalize(this);

            foreach (var subscription in _subscriptions.Values)
                subscription.Observer.OnCompleted();

            _subscriptions.Clear();
        }

        public void Emit(T value)
        {
            foreach (var subscription in _subscriptions)
            {
                try
                {
                    subscription.Value.Observer.OnNext(value);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error in observer while emitting value.", ex);

                    RemoveSubscription(subscription.Key);

                    Swallow.Everything(() => subscription.Value.Observer.OnError(ex));
                }
            }
        }

        public virtual IDisposable Subscribe(IObserver<T> observer)
        {
            ThrowIfDisposed();

            var sub = new ObSubscription(observer, RemoveSubscription);

            _subscriptions[sub.Id] = sub;

            return sub;
        }

        private void RemoveSubscription(Guid subscriptionId)
            => _subscriptions.TryRemove(subscriptionId, out var _);

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        private sealed class ObSubscription : IDisposable
        {
            private readonly Action<Guid> _onDispose;

            public Guid Id { get; }
            public IObserver<T> Observer { get; }

            public ObSubscription(IObserver<T> observer, Action<Guid> onDispose)
            {
                Id = Guid.NewGuid();
                Observer = observer;

                _onDispose = onDispose;
            }

            public void Dispose()
                => _onDispose(Id);
        }
    }
}