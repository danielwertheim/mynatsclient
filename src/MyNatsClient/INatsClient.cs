using System;

namespace NatsFun
{
    public interface INatsClient
    {
        string Id { get; }
        IObservable<IClientEvent> Events { get; }
        IObservable<IOp> IncomingOps { get; }
        INatsClientStats Stats { get; }
        NatsClientState State { get; }

        void Disconnect();
        void Connect();
        void Pub(string subject, string data);
        void Sub(string subject, string subscriptionId);
        void UnSub(string subscriptionId, int? maxMessages = null);
        void Ping();
        void Pong();
    }
}