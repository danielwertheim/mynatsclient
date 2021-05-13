using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyNatsClient
{
    public static class LoggerManager
    {
        private static ILoggerFactory _factory = NullLoggerFactory.Instance;

        public static ILogger CreateLogger(Type type)
            => _factory.CreateLogger(type);

        public static ILogger<T> CreateLogger<T>()
            => _factory.CreateLogger<T>();

        public static void UseFactory(ILoggerFactory factory)
            => _factory = factory;

        public static void ResetToDefaults()
            => UseFactory(NullLoggerFactory.Instance);
    }
}
