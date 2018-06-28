using System;
using MyNatsClient.Logging;

namespace MyNatsClient
{
    public static class LoggerManager
    {
        public static Func<Type, ILogger> Resolve { get; set; } = _ => NullLogger.Instance;

        public static void ResetToDefaults() => UseNullLogger();

        public static void UseNullLogger() => Resolve = _ => NullLogger.Instance;
    }
}