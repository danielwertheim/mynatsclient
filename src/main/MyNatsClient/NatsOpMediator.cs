using System;
using System.Collections.Generic;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public sealed class NatsOpMediator : IDisposable
    {
        private const string DisposeExMessage = "Failed while disposing Op-mediator. See inner exception(s) for more details.";
        
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

            Exception ex1 = null, ex2 = null;

            try
            {
                _opStream.Dispose();
            }
            catch (Exception ex)
            {
                ex1 = ex;
            }

            try
            {
                _msgOpStream.Dispose();
            }
            catch (Exception ex)
            {
                ex2 = ex;
            }

            _opStream = null;
            _msgOpStream = null;

            if(ex1 == null && ex2 == null)
                return;

            if(ex1 != null && ex2 != null)
                throw new AggregateException(DisposeExMessage, ex1, ex2);
            
            if(ex1 != null)
                throw new AggregateException(DisposeExMessage, ex1);
            
            if(ex2 != null)
                throw new AggregateException(DisposeExMessage, ex2);
        }

        public void Emit(IEnumerable<IOp> ops)
        {
            foreach (var op in ops)
                Emit(op);
        }

        public void Emit(IOp op)
        {
            if (op is MsgOp msgOp)
                _msgOpStream.Emit(msgOp);

            _opStream.Emit(op);
        }
    }
}