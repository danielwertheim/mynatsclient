using System;

namespace NatsFun
{
    public interface INatsClientStats
    {
        DateTime LastOpReceivedAt { get; }
        long OpCount { get; }
    }
}