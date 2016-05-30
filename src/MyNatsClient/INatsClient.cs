using System;
using System.Threading.Tasks;

namespace MyNatsClient
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

        void Pub(string subject, string body, string replyTo);
        void Pub(string subject, byte[] body, string replyTo);
        Task PubAsync(string subject, string body, string replyTo);
        Task PubAsync(string subject, byte[] body, string replyTo);

        void Sub(string subject, string subscriptionId, string queueGroup = null);
        Task SubAsync(string subject, string subscriptionId, string queueGroup = null);

        void UnSub(string subscriptionId, int? maxMessages = null);
        Task UnSubAsync(string subscriptionId, int? maxMessages = null);
    }
}