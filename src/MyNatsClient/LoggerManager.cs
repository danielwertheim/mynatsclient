using System;
using NatsFun.Logging;

namespace NatsFun
{
    public static class LoggerManager
    {
        public static Func<Type, ILogger> Resolve { get; } = t => NullLogger.Instance;
    }
}