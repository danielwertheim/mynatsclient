using System;
using Microsoft.Extensions.Logging;

namespace MyNatsClient.Logging
{
    public class MicrosoftLogger : MyNatsClient.ILogger
    {
        private static Microsoft.Extensions.Logging.ILogger _logger;

        public MicrosoftLogger(Microsoft.Extensions.Logging.ILogger logger)
        {
            _logger = logger;
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
