using System;
using MyNatsClient.Logging;
using Microsoft.Extensions.Logging;

namespace MyNatsClient
{
    public static class LoggerManager
    {
        private static Func<Type, MyNatsClient.ILogger> _resolve = _ => NullLogger.Instance;

        public static Func<Type, MyNatsClient.ILogger> Resolve
        {
            set { if (value != null) _resolve = value; }
            get => _resolve;
        }

        public static void ResetToDefaults() => UseNullLogger();

        public static void UseNullLogger() => Resolve = _ => NullLogger.Instance;

        public static void UseMicrosoftLogger(Microsoft.Extensions.Logging.ILogger logger)
        {
            if (logger != null)
            {
                MicrosoftLogger.Instance.SetInternalLogger(logger);

                Resolve = _ => MicrosoftLogger.Instance;
            }
            else
            {
                throw new ArgumentNullException(nameof(logger));
            }
        }
    }
}
