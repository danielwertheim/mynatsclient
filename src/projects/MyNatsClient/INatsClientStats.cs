using System;

namespace MyNatsClient
{
    public interface INatsClientStats
    {
        DateTime LastOpReceivedAt { get; }
        ulong OpCount { get; }
    }
}