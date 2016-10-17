using System;
using System.Threading.Tasks;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public interface INatsConsumer
    {
        /// <summary>
        /// Creates a <see cref="IConsumerSubscription"/> that consumes <see cref="MsgOp"/> stream for a certain subject.
        /// </summary>
        /// <param name="subject">The subject that this subscription should be consuming.</param>
        /// <param name="handler">The action that will be invoked by observer for each <see cref="MsgOp"/> message in the stream.</param>
        /// <returns></returns>
        IConsumerSubscription Subscribe(string subject, Action<MsgOp> handler);

        /// <summary>
        /// Creates a <see cref="IConsumerSubscription"/> that consumes <see cref="MsgOp"/> stream for a certain subject.
        /// </summary>
        /// <param name="subject">The subject that this subscription should be consuming.</param>
        /// <param name="observer">The observer that will observe the stream of <see cref="MsgOp"/> for this subscription.</param>
        /// <returns></returns>
        IConsumerSubscription Subscribe(string subject, IObserver<MsgOp> observer);

        /// <summary>
        /// Creates a <see cref="IConsumerSubscription"/> that consumes <see cref="MsgOp"/> stream for a certain subject.
        /// </summary>
        /// <param name="subscriptionInfo">The Subscrition info indicating what subject etc. that this subscription should be consuming.</param>
        /// <param name="handler">The action that will be invoked by observer for each <see cref="MsgOp"/> message in the stream.</param>
        /// <returns></returns>
        IConsumerSubscription Subscribe(SubscriptionInfo subscriptionInfo, Action<MsgOp> handler);

        /// <summary>
        /// Creates a <see cref="IConsumerSubscription"/> that consumes <see cref="MsgOp"/> stream for a certain subject.
        /// </summary>
        /// <param name="subscriptionInfo">The Subscrition info indicating what subject etc. that this subscription should be consuming.</param>
        /// <param name="observer">The observer that will observe the stream of <see cref="MsgOp"/> for this subscription.</param>
        /// <returns></returns>
        IConsumerSubscription Subscribe(SubscriptionInfo subscriptionInfo, IObserver<MsgOp> observer);

        /// <summary>
        /// Creates a <see cref="IConsumerSubscription"/> that consumes <see cref="MsgOp"/> stream for a certain subject.
        /// </summary>
        /// <param name="subject">The subject that this subscription should be consuming.</param>
        /// <param name="observer">The observer that will observe the stream of <see cref="MsgOp"/> for this subscription.</param>
        /// <returns></returns>
        Task<IConsumerSubscription> SubscribeAsync(string subject, IObserver<MsgOp> observer);

        /// <summary>
        /// Creates a <see cref="IConsumerSubscription"/> that consumes <see cref="MsgOp"/> stream for a certain subject.
        /// </summary>
        /// <param name="subject">The subject that this subscription should be consuming.</param>
        /// <param name="handler">The action that will be invoked by observer for each <see cref="MsgOp"/> message in the stream.</param>
        /// <returns></returns>
        Task<IConsumerSubscription> SubscribeAsync(string subject, Action<MsgOp> handler);

        /// <summary>
        /// Creates a <see cref="IConsumerSubscription"/> that consumes <see cref="MsgOp"/> stream for a certain subject.
        /// </summary>
        /// <param name="subscriptionInfo">The Subscrition info indicating what subject etc. that this subscription should be consuming.</param>
        /// <param name="observer">The observer that will observe the stream of <see cref="MsgOp"/> for this subscription.</param>
        /// <returns></returns>
        Task<IConsumerSubscription> SubscribeAsync(SubscriptionInfo subscriptionInfo, IObserver<MsgOp> observer);

        /// <summary>
        /// Creates a <see cref="IConsumerSubscription"/> that consumes <see cref="MsgOp"/> stream for a certain subject.
        /// </summary>
        /// <param name="subscriptionInfo">The Subscrition info indicating what subject etc. that this subscription should be consuming.</param>
        /// <param name="handler">The action that will be invoked by observer for each <see cref="MsgOp"/> message in the stream.</param>
        /// <returns></returns>
        Task<IConsumerSubscription> SubscribeAsync(SubscriptionInfo subscriptionInfo, Action<MsgOp> handler);

        /// <summary>
        /// Unsubscribe of previously registrered subscription.
        /// </summary>
        /// <param name="subscription"></param>
        void Unsubscribe(IConsumerSubscription subscription);

        /// <summary>
        /// Async unsubscribe of previously registrered subscription.
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        Task UnsubscribeAsync(IConsumerSubscription subscription);
    }
}