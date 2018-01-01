using System;
using MyNatsClient.Internals;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public class NatsOpMediator : IDisposable
    {
        private bool _isDisposed;
        private SafeObservableOf<IOp> _opStream;
        private SafeObservableOf<MsgOp> _msgOpStream;

        public IObservable<IOp> AllOpsStream => _opStream;
        public IObservable<MsgOp> MsgOpsStream => _msgOpStream;

        public NatsOpMediator()
        {
            _opStream = new SafeObservableOf<IOp>();
            _msgOpStream = new SafeObservableOf<MsgOp>();
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

        public void Emit(IOp op)
        {
            if (op is MsgOp msgOp)
                _msgOpStream.Emit(msgOp);

            _opStream.Emit(op);
        }
    }
}