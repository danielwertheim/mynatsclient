using System;
using MyNatsClient.Internals;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public class NatsOpMediator :
        IObservable<IOp>,
        IObservable<MsgOp>,
        INatsClientStats,
        IDisposable
    {
        private bool _isDisposed;
        private ObservableOf<IOp> _opStream = new ObservableOf<IOp>();
        private ObservableOf<MsgOp> _msgOpStream = new ObservableOf<MsgOp>();

        public DateTime LastOpReceivedAt { get; private set; }
        public long OpCount { get; private set; }

        public IDisposable Subscribe(IObserver<IOp> observer)
        {
            return _opStream.Subscribe(observer);
        }

        public IDisposable Subscribe(IObserver<MsgOp> observer)
        {
            return _msgOpStream.Subscribe(observer);
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

            Try.DisposeAll(_opStream, _msgOpStream);
            _opStream = null;
            _msgOpStream = null;
        }
    }
}