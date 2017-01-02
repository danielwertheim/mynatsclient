using System;
using MyNatsClient.Internals;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public class NatsOpMediator :
        INatsClientStats,
        IDisposable
    {
        private bool _isDisposed;
        private ObservableOf<IOp> _opStream;
        private ObservableOf<MsgOp> _msgOpStream;

        public IFilterableObservable<IOp> AllOpsStream => _opStream;
        public IFilterableObservable<MsgOp> MsgOpsStream => _msgOpStream;
        public DateTime LastOpReceivedAt { get; private set; }
        public long OpCount { get; private set; }

        public NatsOpMediator(bool autoRemoveFailingSubscription)
        {
            _opStream = new ObservableOf<IOp>(autoRemoveFailingSubscription);
            _msgOpStream = new ObservableOf<MsgOp>(autoRemoveFailingSubscription);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            GC.SuppressFinalize(this);

            Try.DisposeAll(_opStream, _msgOpStream);
            _opStream = null;
            _msgOpStream = null;
        }

        public void Dispatch(IOp op)
        {
            LastOpReceivedAt = DateTime.UtcNow;
            OpCount++;

            var msgOp = op as MsgOp;
            if (msgOp != null)
                _msgOpStream.Dispatch(msgOp);

            _opStream.Dispatch(op);
        }
    }
}