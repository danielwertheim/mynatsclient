using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyNatsClient
{
    public static class LoggerManager
    {
        private static ILoggerFactory Factory { get; set; } = NullLoggerFactory.Instance;

        public static ILogger CreateLogger(Type type)
            => Factory.CreateLogger(type);

        public static ILogger<T> CreateLogger<T>()
            => Factory.CreateLogger<T>();

        public static void UseFactory(ILoggerFactory factory)
            => Factory = factory;

        public static void ResetToDefaults()
            => UseFactory(NullLoggerFactory.Instance);
    }
}
