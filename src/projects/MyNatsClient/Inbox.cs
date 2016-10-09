using System;
using EnsureThat;
using MyNatsClient.Internals;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public class Inbox : IDisposable
    {
        private IDisposable _subscription;
        private bool _isDisposed;

        public string Subject { get; }
        public string SubscriptionId { get; }
        public IFilterableObservable<MsgOp> MessageStream { get; }

        public Inbox(string subject, IFilterableObservable<MsgOp> messageStream, IObserver<MsgOp> observer)
        {
            EnsureArg.IsNotNullOrWhiteSpace(subject, nameof(subject));
            EnsureArg.IsNotNull(messageStream, nameof(messageStream));

            Subject = subject;
            SubscriptionId = Guid.NewGuid().ToString("N");
            MessageStream = messageStream;
            _subscription = messageStream.Subscribe(observer, ev => ev.Subject == subject);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            _isDisposed = true;
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed || !disposing)
                return;

            Try.DisposeAll(_subscription);
            _subscription = null;
        }
    }
}