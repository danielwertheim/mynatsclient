using System;
using System.Threading.Tasks;

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

        void Ping();
        Task PingAsync();

        void Pong();
        Task PongAsync();

        void Pub(string subject, string data);
        Task PubAsync(string subject, string data);

        void Pub(string subject, string replyTo, string data);
        Task PubAsync(string subject, string replyTo, string data);

        void Sub(string subject, string subscriptionId);
        Task SubAsync(string subject, string subscriptionId);

        void Sub(string subject, string queueGroup, string subscriptionId);
        Task SubAsync(string subject, string queueGroup, string subscriptionId);

        void UnSub(string subscriptionId, int? maxMessages = null);
        Task UnSubAsync(string subscriptionId, int? maxMessages = null);

        void Send(string data);
        Task SendAsync(string data);
    }
}