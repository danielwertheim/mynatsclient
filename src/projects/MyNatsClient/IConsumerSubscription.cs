using System;

namespace MyNatsClient
{
    public interface IConsumerSubscription : IDisposable
    {
        SubscriptionInfo SubscriptionInfo { get; }
    }
}