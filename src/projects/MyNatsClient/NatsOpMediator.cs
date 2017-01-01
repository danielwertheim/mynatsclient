using System;
using MyNatsClient.Internals;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public class NatsOpMediator :
        IObservable<IOp>,
        IFilterableObservable<MsgOp>,
        INatsClientStats,
        IDisposable
    {
        private bool _isDisposed;
        private ObservableOf<IOp> _opStream;
        private ObservableOf<MsgOp> _msgOpStream;

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

        public IDisposable Subscribe(IObserver<IOp> observer)
        {
            return _opStream.Subscribe(observer);
        }

        public IDisposable Subscribe(IObserver<MsgOp> observer)
        {
            return _msgOpStream.Subscribe(observer);
        }

        public IDisposable Subscribe(IObserver<MsgOp> observer, Func<MsgOp, bool> filter)
        {
            return _msgOpStream.Subscribe(observer, filter);
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