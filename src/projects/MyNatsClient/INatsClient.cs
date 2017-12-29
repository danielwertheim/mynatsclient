using System;
using System.Threading.Tasks;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public interface INatsClient
    {
        /// <summary>
        /// Stream of client events that mostly concerns client state changes.
        /// E.g.
        /// <see cref="MyNatsClient.Events.ClientConnected"/>,
        /// <see cref="MyNatsClient.Events.ClientDisconnected"/>,
        /// <see cref="MyNatsClient.Events.ClientConsumerFailed"/>.
        /// </summary>
        INatsObservable<IClientEvent> Events { get; }

        /// <summary>
        /// Stream of all incoming Ops.
        /// E.g.
        /// <see cref="ErrOp"/>,
        /// <see cref="InfoOp"/>,
        /// <see cref="PingOp"/>,
        /// <see cref="PongOp"/>,
        /// <see cref="MsgOp"/>
        /// </summary>
        INatsObservable<IOp> OpStream { get; }

        /// <summary>
        /// Stream of all incoming <see cref="MsgOp"/>.
        /// </summary>
        INatsObservable<MsgOp> MsgOpStream { get; }

        /// <summary>
        /// Gets client statistics.
        /// </summary>
        INatsClientStats Stats { get; }

        /// <summary>
        /// Gets value indicating if the client is connected or not.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Connects the client to one of the <see cref="Host"/>
        /// specified in <see cref="ConnectionInfo"/>.
        /// </summary>
        void Connect();

        /// <summary>
        /// Disconnects the client.
        /// </summary>
        void Disconnect();

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
        void Pub(string subject, string body, string replyTo = null);

        /// <summary>
        /// Sync Publish of a message.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="replyTo"></param>
        void Pub(string subject, byte[] body, string replyTo = null);

        /// <summary>
        /// Async Publish of a messages using <see cref="IPayload"/>.
        /// This is intended for use when dispatching larger messages.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="replyTo"></param>
        void Pub(string subject, IPayload body, string replyTo = null);

        /// <summary>
        /// Async Publish of a message.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="replyTo"></param>
        /// <returns></returns>
        Task PubAsync(string subject, string body, string replyTo = null);

        /// <summary>
        /// Async Publish of a message.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="replyTo"></param>
        /// <returns></returns>
        Task PubAsync(string subject, byte[] body, string replyTo = null);

        /// <summary>
        /// Async Publish of a messages using <see cref="IPayload"/>.
        /// This is intended for use when dispatching larger messages.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="replyTo"></param>
        /// <returns></returns>
        Task PubAsync(string subject, IPayload body, string replyTo = null);

        /// <summary>
        /// Gives access to a publisher that will be running in
        /// a sync locked scope until your injected delegate
        /// is done.
        /// </summary>
        /// <param name="p"></param>
        void PubMany(Action<IPublisher> p);

        /// <summary>
        /// Async request response.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="timeoutMs"></param>
        Task<MsgOp> RequestAsync(string subject, string body, int? timeoutMs = null);

        /// <summary>
        /// Async request response.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="timeoutMs"></param>
        Task<MsgOp> RequestAsync(string subject, byte[] body, int? timeoutMs = null);

        /// <summary>
        /// Async request response.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="timeoutMs"></param>
        Task<MsgOp> RequestAsync(string subject, IPayload body, int? timeoutMs = null);

        /// <summary>
        /// Creates a <see cref="ISubscription"/> which sets up a subscription against the NATS server.
        /// </summary>
        /// <param name="subject">The subject that this subscription should be consuming.</param>
        /// <returns></returns>
        /// <remarks>You still need to setup a manual subscription against <see cref="MsgOpStream"/> with a handler.</remarks>
        ISubscription Sub(string subject);

        /// <summary>
        /// Creates a <see cref="ISubscription"/> which sets up a subscription against the NATS server.
        /// Also sets up a subscripion that consumes <see cref="MsgOp"/> stream using passed <paramref name="handler"/>.
        /// </summary>
        /// <param name="subject">The subject that this subscription should be consuming.</param>
        /// <param name="handler">The action that will be invoked by observer for each <see cref="MsgOp"/> message in the stream.</param>
        /// <returns></returns>
        ISubscription Sub(string subject, Action<MsgOp> handler);

        /// <summary>
        /// Creates a <see cref="ISubscription"/> which sets up a subscription against the NATS server.
        /// Also sets up a subscripion that consumes <see cref="MsgOp"/> stream using passed <paramref name="observer"/>.
        /// </summary>
        /// <param name="subject">The subject that this subscription should be consuming.</param>
        /// <param name="observer">The observer that will observe the stream of <see cref="MsgOp"/> for this subscription.</param>
        /// <returns></returns>
        ISubscription Sub(string subject, IObserver<MsgOp> observer);

        /// <summary>
        /// Creates a <see cref="ISubscription"/> which sets up a subscription against the NATS server.
        /// Also sets up a subscripion that consumes <see cref="MsgOp"/>.
        /// </summary>
        /// <param name="subject">The subject that this subscription should be consuming.</param>
        /// <param name="subscriptionFactory">Should return a disposable subscription that will be invoked when unsub is perfored.</param>
        /// <returns></returns>
        ISubscription Sub(string subject, Func<INatsObservable<MsgOp>, IDisposable> subscriptionFactory);

        /// <summary>
        /// Creates a <see cref="ISubscription"/> which sets up a subscription against the NATS server.
        /// </summary>
        /// <param name="subscriptionInfo">The Subscrition info indicating what subject etc. that this subscription should be consuming.</param>
        /// <returns></returns>
        /// <remarks>You still need to setup a manual subscription against <see cref="MsgOpStream"/> with a handler.</remarks>
        ISubscription Sub(SubscriptionInfo subscriptionInfo);

        /// <summary>
        /// Creates a <see cref="ISubscription"/> which sets up a subscription against the NATS server.
        /// Also sets up a subscripion that consumes <see cref="MsgOp"/> stream using passed <paramref name="handler"/>.
        /// </summary>
        /// <param name="subscriptionInfo">The Subscrition info indicating what subject etc. that this subscription should be consuming.</param>
        /// <param name="handler">The action that will be invoked by observer for each <see cref="MsgOp"/> message in the stream.</param>
        /// <returns></returns>
        ISubscription Sub(SubscriptionInfo subscriptionInfo, Action<MsgOp> handler);

        /// <summary>
        /// Creates a <see cref="ISubscription"/> which sets up a subscription against the NATS server.
        /// Also sets up a subscripion that consumes <see cref="MsgOp"/> stream using passed <paramref name="observer"/>.
        /// </summary>
        /// <param name="subscriptionInfo">The Subscrition info indicating what subject etc. that this subscription should be consuming.</param>
        /// <param name="observer">The observer that will observe the stream of <see cref="MsgOp"/> for this subscription.</param>
        /// <returns></returns>
        ISubscription Sub(SubscriptionInfo subscriptionInfo, IObserver<MsgOp> observer);

        /// <summary>
        /// Creates a <see cref="ISubscription"/> which sets up a subscription against the NATS server.
        /// Also sets up a subscripion that consumes <see cref="MsgOp"/> stream.
        /// </summary>
        /// <param name="subscriptionInfo">The Subscrition info indicating what subject etc. that this subscription should be consuming.</param>
        /// <param name="subscriptionFactory">Should return a disposable subscription that will be invoked when unsub is perfored.</param>
        /// <returns></returns>
        ISubscription Sub(SubscriptionInfo subscriptionInfo, Func<INatsObservable<MsgOp>, IDisposable> subscriptionFactory);

        /// <summary>
        /// Creates a <see cref="ISubscription"/> which sets up a subscription against the NATS server.
        /// </summary>
        /// <param name="subject">The subject that this subscription should be consuming.</param>
        /// <returns></returns>
        /// <remarks>You still need to setup a manual subscription against <see cref="MsgOpStream"/> with a handler.</remarks>
        Task<ISubscription> SubAsync(string subject);

        /// <summary>
        /// Creates a <see cref="ISubscription"/> which sets up a subscription against the NATS server.
        /// Also sets up a subscripion that consumes <see cref="MsgOp"/> stream using passed <paramref name="handler"/>.
        /// </summary>
        /// <param name="subject">The subject that this subscription should be consuming.</param>
        /// <param name="handler">The action that will be invoked by observer for each <see cref="MsgOp"/> message in the stream.</param>
        /// <returns></returns>
        Task<ISubscription> SubAsync(string subject, Action<MsgOp> handler);

        /// <summary>
        /// Creates a <see cref="ISubscription"/> which sets up a subscription against the NATS server.
        /// Also sets up a subscripion that consumes <see cref="MsgOp"/> stream using passed <paramref name="observer"/>.
        /// </summary>
        /// <param name="subject">The subject that this subscription should be consuming.</param>
        /// <param name="observer">The observer that will observe the stream of <see cref="MsgOp"/> for this subscription.</param>
        /// <returns></returns>
        Task<ISubscription> SubAsync(string subject, IObserver<MsgOp> observer);

        /// <summary>
        /// Creates a <see cref="ISubscription"/> which sets up a subscription against the NATS server.
        /// Also sets up a subscripion that consumes <see cref="MsgOp"/> stream.
        /// </summary>
        /// <param name="subject">The subject that this subscription should be consuming.</param>
        /// <param name="subscriptionFactory">Should return a disposable subscription that will be invoked when unsub is perfored.</param>
        /// <returns></returns>
        Task<ISubscription> SubAsync(string subject, Func<INatsObservable<MsgOp>, IDisposable> subscriptionFactory);

        /// <summary>
        /// Creates a <see cref="ISubscription"/> which sets up a subscription against the NATS server.
        /// </summary>
        /// <param name="subscriptionInfo">The Subscrition info indicating what subject etc. that this subscription should be consuming.</param>
        /// <returns></returns>
        /// <remarks>You still need to setup a manual subscription against <see cref="MsgOpStream"/> with a handler.</remarks>
        Task<ISubscription> SubAsync(SubscriptionInfo subscriptionInfo);

        /// <summary>
        /// Creates a <see cref="ISubscription"/> which sets up a subscription against the NATS server.
        /// Also sets up a subscripion that consumes <see cref="MsgOp"/> stream using passed <paramref name="handler"/>.
        /// </summary>
        /// <param name="subscriptionInfo">The Subscrition info indicating what subject etc. that this subscription should be consuming.</param>
        /// <param name="handler">The action that will be invoked by observer for each <see cref="MsgOp"/> message in the stream.</param>
        /// <returns></returns>
        Task<ISubscription> SubAsync(SubscriptionInfo subscriptionInfo, Action<MsgOp> handler);

        /// <summary>
        /// Creates a <see cref="ISubscription"/> which sets up a subscription against the NATS server.
        /// Also sets up a subscripion that consumes <see cref="MsgOp"/> stream using passed <paramref name="observer"/>.
        /// </summary>
        /// <param name="subscriptionInfo">The Subscrition info indicating what subject etc. that this subscription should be consuming.</param>
        /// <param name="observer">The observer that will observe the stream of <see cref="MsgOp"/> for this subscription.</param>
        /// <returns></returns>
        Task<ISubscription> SubAsync(SubscriptionInfo subscriptionInfo, IObserver<MsgOp> observer);

        /// <summary>
        /// Creates a <see cref="ISubscription"/> which sets up a subscription against the NATS server.
        /// Also sets up a subscripion that consumes <see cref="MsgOp"/> stream.
        /// </summary>
        /// <param name="subscriptionInfo">The Subscrition info indicating what subject etc. that this subscription should be consuming.</param>
        /// <param name="subscriptionFactory">Should return a disposable subscription that will be invoked when unsub is perfored.</param>
        /// <returns></returns>
        Task<ISubscription> SubAsync(SubscriptionInfo subscriptionInfo, Func<INatsObservable<MsgOp>, IDisposable> subscriptionFactory);

        /// <summary>
        /// Unsubscribes from the server as well as any previosly created <see cref="ISubscription"/>
        /// with any associated subscription (handler, observer) against <see cref="MsgOpStream"/>.
        /// </summary>
        /// <param name="subscriptionInfo"></param>
        void Unsub(SubscriptionInfo subscriptionInfo);

        /// <summary>
        /// Unsubscribes from the server as well as any previosly created <see cref="ISubscription"/>
        /// with any associated subscription (handler, observer) against <see cref="MsgOpStream"/>.
        /// </summary>
        /// <param name="subscriptionInfo"></param>
        Task UnsubAsync(SubscriptionInfo subscriptionInfo);
    }
}