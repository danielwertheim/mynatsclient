using System;
using System.Threading.Tasks;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public interface INatsClient
    {
        /// <summary>
        /// ClientId. Currently not used more than for user convenience,
        /// like if you have multiple clients running and want to keep
        /// them apart.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Stream of client events that mostly concerns client state changes.
        /// E.g.
        /// <see cref="MyNatsClient.Events.ClientConnected"/>,
        /// <see cref="MyNatsClient.Events.ClientDisconnected"/>,
        /// <see cref="MyNatsClient.Events.ClientConsumerFailed"/>.
        /// </summary>
        IObservable<IClientEvent> Events { get; }

        /// <summary>
        /// Stream of all incoming Ops.
        /// E.g.
        /// <see cref="ErrOp"/>,
        /// <see cref="InfoOp"/>,
        /// <see cref="PingOp"/>,
        /// <see cref="PongOp"/>,
        /// <see cref="MsgOp"/>
        /// </summary>
        IObservable<IOp> OpStream { get; }

        /// <summary>
        /// Stream of all incoming <see cref="MsgOp"/>.
        /// </summary>
        IObservable<MsgOp> MsgOpStream { get; }

        /// <summary>
        /// Gets client statistics.
        /// </summary>
        INatsClientStats Stats { get; }

        /// <summary>
        /// Gets current State of the client. <see cref="NatsClientState"/>.
        /// </summary>
        NatsClientState State { get; }

        /// <summary>
        /// Disconnects the client.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Connects the client to one of the <see cref="Host"/>
        /// specified in <see cref="ConnectionInfo"/>.
        /// </summary>
        void Connect();

        /// <summary>
        /// Sync send of a Ping message to the server, which then
        /// should reply with a Pong.
        /// </summary>
        void Ping();

        /// <summary>
        /// Async send of a Ping message to the server, which then
        /// should reply with a Pong.
        /// </summary>
        /// <returns></returns>
        Task PingAsync();

        /// <summary>
        /// Sync send of a Pong message to the server as a reply on servers Ping, so that you
        /// the server does not cut this client off.
        /// This is taken care automatically by the client if you specify
        /// <see cref="ConnectionInfo.AutoRespondToPing"/>.
        /// </summary>
        void Pong();

        /// <summary>
        /// Async send of a Pong message to the server as a reply on servers Ping, so that you
        /// the server does not cut this client off.
        /// This is taken care automatically by the client if you specify
        /// <see cref="ConnectionInfo.AutoRespondToPing"/>.
        /// </summary>
        Task PongAsync();

        /// <summary>
        /// Sync Publish of a message.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="replyTo"></param>
        void Pub(string subject, string body, string replyTo);

        /// <summary>
        /// Sync Publish of a message.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="replyTo"></param>
        void Pub(string subject, byte[] body, string replyTo);

        /// <summary>
        /// Async Publish of a message.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="replyTo"></param>
        /// <returns></returns>
        Task PubAsync(string subject, string body, string replyTo);

        /// <summary>
        /// Async Publish of a message.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="replyTo"></param>
        /// <returns></returns>
        Task PubAsync(string subject, byte[] body, string replyTo);

        /// <summary>
        /// Gives access to a publisher that will call
        /// <see cref="Flush"/> when your inject delegate
        /// is done executing.
        /// </summary>
        /// <param name="p"></param>
        void PubMany(Action<IPublisher> p);

        /// <summary>
        /// Flushes the write stream.
        /// </summary>
        void Flush();

        /// <summary>
        /// Async flush of write stream.
        /// </summary>
        /// <returns></returns>
        Task FlushAsync();

        /// <summary>
        /// Sync send of Sub message to indicate that client should
        /// get messages for the subject.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="queueGroup"></param>
        void Sub(string subject, string subscriptionId, string queueGroup = null);

        /// <summary>
        /// Async send of Sub message to indicate that the client
        /// should get messages for the subject.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="queueGroup"></param>
        /// <returns></returns>
        Task SubAsync(string subject, string subscriptionId, string queueGroup = null);

        /// <summary>
        /// Sync send of UnSub message to indicate that the client
        /// should not receive messages anymore for the specific subject.
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="maxMessages"></param>
        void UnSub(string subscriptionId, int? maxMessages = null);

        /// <summary>
        /// Async send of UnSub message to indicate that the client
        /// should not receive messages anymore for the specific subject.
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="maxMessages"></param>
        /// <returns></returns>
        Task UnSubAsync(string subscriptionId, int? maxMessages = null);
    }
}