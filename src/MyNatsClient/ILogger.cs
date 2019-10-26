using System;

namespace MyNatsClient
{
    public interface ILogger
    {
        void Trace(string message);
        void Debug(string message);
        void Info(string message);
        void Error(string message);
        void Error(string message, Exception ex);
    }
}