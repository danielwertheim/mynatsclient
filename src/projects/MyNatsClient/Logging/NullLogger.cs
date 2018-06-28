using System;

namespace MyNatsClient.Logging
{
    public class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new NullLogger();

        private NullLogger() { }

        public void Trace(string message) { }
        public void Debug(string message) { }
        public void Info(string message) { }
        public void Error(string message) { }
        public void Error(string message, Exception ex) { }
    }
}