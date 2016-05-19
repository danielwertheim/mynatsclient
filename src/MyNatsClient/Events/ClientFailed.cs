using NatsFun.Ops;

namespace NatsFun.Events
{
    public class ClientFailed : IClientEvent
    {
        public INatsClient Client { get; }
        public ErrOp ErrOp { get; }

        public ClientFailed(INatsClient client, ErrOp errOp)
        {
            Client = client;
            ErrOp = errOp;
        }
    }
}