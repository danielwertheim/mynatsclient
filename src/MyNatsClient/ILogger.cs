using System;

namespace NatsFun
{
    public interface ILogger
    {
        void Info(string message);
        void Error(string message);
        void Fatal(string message, Exception ex);
    }
}