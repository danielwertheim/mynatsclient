using System;
using EnsureThat;
using MyNatsClient.Ops;

namespace MyNatsClient.Internals
{
    internal class ClientInbox : IDisposable
    {
        private bool _isDisposed;

        private ISubscription _inboxSubscription;
        private ObservableOf<MsgOp> _responses;

        public string Address { get; }
        public IFilterableObservable<MsgOp> Responses => _responses;

        internal ClientInbox(INatsClient client)
        {
            EnsureArg.IsNotNull(client, nameof(client));

            Address = Guid.NewGuid().ToString("N");
            _responses = new ObservableOf<MsgOp>();
            _inboxSubscription = client.SubWithHandler($"{Address}.>", msg => _responses.Dispatch(msg));
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            Dispose(true);
            GC.SuppressFinalize(this);
            _isDisposed = true;
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed || !disposing)
                return;

            Try.All(
                () =>
                {
                    _inboxSubscription?.Dispose();
                    _inboxSubscription = null;
                },
                () =>
                {
                    _responses?.Dispose();
                    _responses = null;
                });
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}