using System;

namespace MyNatsClient
{
    public interface ISubscription : IDisposable
    {
        SubscriptionInfo SubscriptionInfo { get; }
    }
}