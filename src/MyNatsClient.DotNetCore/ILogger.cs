using System;

namespace MyNatsClient
{
    public interface ILogger
    {
        void Debug(string message);
        void Info(string message);
        void Error(string message);
        void Error(string message, Exception ex);
    }
}