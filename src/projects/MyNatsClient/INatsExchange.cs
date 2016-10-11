using System;
using System.Threading.Tasks;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public interface INatsExchange
    {
        /// <summary>
        /// Gets the underlying client that you can use to access lowest level features.
        /// </summary>
        INatsClient Client { get; }

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
        /// Creates a <see cref="ISubscription"/> that consumes <see cref="MsgOp"/> stream for a certain subject.
        /// </summary>
        /// <param name="subject">The subject that this subscription should be consuming.</param>
        /// <param name="observer">The observer that will observe the stream of <see cref="MsgOp"/> for this subscription.</param>
        /// <param name="unsubAfterNMessages">Pass a value to automatically unsubscribe after N messages.</param>
        /// <returns></returns>
        ISubscription Subscribe(string subject, IObserver<MsgOp> observer, int? unsubAfterNMessages = null);

        /// <summary>
        /// Creates a <see cref="ISubscription"/> that consumes <see cref="MsgOp"/> stream for a certain subject.
        /// </summary>
        /// <param name="subscriptionInfo">The Subscrition info indicating what subject etc. that this subscription should be consuming.</param>
        /// <param name="observer">The observer that will observe the stream of <see cref="MsgOp"/> for this subscription.</param>
        /// <param name="unsubAfterNMessages">Pass a value to automatically unsubscribe after N messages.</param>
        /// <returns></returns>
        ISubscription Subscribe(SubscriptionInfo subscriptionInfo, IObserver<MsgOp> observer, int? unsubAfterNMessages = null);

        /// <summary>
        /// Creates a <see cref="ISubscription"/> that consumes <see cref="MsgOp"/> stream for a certain subject.
        /// </summary>
        /// <param name="subject">The subject that this subscription should be consuming.</param>
        /// <param name="observer">The observer that will observe the stream of <see cref="MsgOp"/> for this subscription.</param>
        /// <param name="unsubAfterNMessages">Pass a value to automatically unsubscribe after N messages.</param>
        /// <returns></returns>
        Task<ISubscription> SubscribeAsync(string subject, IObserver<MsgOp> observer, int? unsubAfterNMessages = null);

        /// <summary>
        /// Creates a <see cref="ISubscription"/> that consumes <see cref="MsgOp"/> stream for a certain subject.
        /// </summary>
        /// <param name="subscriptionInfo">The Subscrition info indicating what subject etc. that this subscription should be consuming.</param>
        /// <param name="observer">The observer that will observe the stream of <see cref="MsgOp"/> for this subscription.</param>
        /// <param name="unsubAfterNMessages">Pass a value to automatically unsubscribe after N messages.</param>
        /// <returns></returns>
        Task<ISubscription> SubscribeAsync(SubscriptionInfo subscriptionInfo, IObserver<MsgOp> observer, int? unsubAfterNMessages = null);
    }
}