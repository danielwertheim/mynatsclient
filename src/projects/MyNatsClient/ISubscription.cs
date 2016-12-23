using System;

namespace MyNatsClient
{
    /// <summary>
    /// Represents a subscription against a NATS broker as well
    /// as an associated message handler against the in-process
    /// observable message stream.
    /// </summary>
    /// <remarks>
    /// If you forget to explicitly dispose, the <see cref="INatsClient"/>
    /// that created this subscribtion, will clean it when disposed.
    /// </remarks>
    public interface ISubscription : IDisposable
    {
        SubscriptionInfo SubscriptionInfo { get; }
    }
}