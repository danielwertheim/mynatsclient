using System;
using Microsoft.Extensions.Logging;

namespace MyNatsClient.Logging
{
    public class MicrosoftLogger : MyNatsClient.ILogger
    {
        private static Microsoft.Extensions.Logging.ILogger _logger = null;

        // System.Lazy is thread-safe by default
        private static readonly Lazy<MicrosoftLogger> lazy = new(() => new MicrosoftLogger());

        internal static MicrosoftLogger Instance => lazy.Value;

        private MicrosoftLogger() { }

        public MicrosoftLogger(Microsoft.Extensions.Logging.ILogger logger)
        {
            SetInternalLogger(logger);
        }

        public void SetInternalLogger(Microsoft.Extensions.Logging.ILogger logger)
        {
            if (logger != null)
            {
                _logger = logger;
            }
            else
            {
                throw new ArgumentNullException(nameof(logger));
            }
        }

        public void Trace(string message)
        {
            _logger?.LogTrace(message);
        }

        public void Debug(string message)
        {
            _logger?.LogDebug(message);
        }

        public void Info(string message)
        {
            _logger?.LogInformation(message);
        }

        public void Error(string message)
        {
            _logger?.LogError(message);
        }

        public void Error(string message, Exception ex)
        {
            _logger?.LogTrace(ex, message);
        }
    }
}
