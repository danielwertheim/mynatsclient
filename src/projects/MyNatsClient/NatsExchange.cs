using System;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public class NatsExchange : IDisposable, INatsExchange
    {
        private bool _isDisposed;
        private NatsClient _client;

        public INatsClient Client => _client;

        public NatsExchange(string id, ConnectionInfo connectionInfo)
        {
            _client = new NatsClient(id, connectionInfo);
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

            _client?.Disconnect();
            _client?.Dispose();
            _client = null;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        public void Connect() => Client.Connect();

        public void Disconnect() => Client.Disconnect();

        public Inbox CreateInbox(string subject, Action<MsgOp> onIncoming, int? unsubAfterNMessages = null)
        {
            ThrowIfDisposed();

            var inbox = new Inbox(subject, Client.MsgOpStream, new DelegatingObserver<MsgOp>(onIncoming));

            Client.Sub(inbox.Subject, inbox.SubscriptionId);

            //TODO: Keep track of inboxes in concurrent dictionary

            return inbox;
        }
    }
}