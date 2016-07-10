using System;

namespace MyNatsClient
{
    public interface INatsClientStats
    {
        DateTime LastOpReceivedAt { get; }
        long OpCount { get; }
    }
}