using System;

namespace MyNatsClient
{
    /// <summary>
    /// Represents a subscription against a NATS broker as well
    /// as an associated message handler against the in process
    /// observable message stream.
    /// </summary>
    /// <remarks>
    /// If you forget to explicitly dispose, the <see cref="INatsConsumer"/>
    /// that created this subscribtion, will clean it.
    /// </remarks>
    public interface IConsumerSubscription : IDisposable
    {
        SubscriptionInfo SubscriptionInfo { get; }
    }
}