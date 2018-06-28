using System;
using MyNatsClient.Internals;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public class NatsOpMediator : IDisposable
    {
        private bool _isDisposed;
        private NatsObservableOf<IOp> _opStream;
        private NatsObservableOf<MsgOp> _msgOpStream;

        public INatsObservable<IOp> AllOpsStream => _opStream;
        public INatsObservable<MsgOp> MsgOpsStream => _msgOpStream;

        public NatsOpMediator()
        {
            _opStream = new NatsObservableOf<IOp>();
            _msgOpStream = new NatsObservableOf<MsgOp>();
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