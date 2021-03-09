using System;

namespace MyNatsClient
{
    /// <inheritdoc />
    /// <summary>
    /// Represents a subscription against a NATS broker as well
    /// as an associated message handler against the in-process
    /// observable message stream.
    /// </summary>
    /// <remarks>
    /// If you forget to explicitly dispose, the <see cref="T:MyNatsClient.INatsClient" />
    /// that created this subscribtion, will clean the subscription when it is disposed.
    /// </remarks>
    public interface ISubscription : IDisposable
    {
        SubscriptionInfo SubscriptionInfo { get; }
    }
}