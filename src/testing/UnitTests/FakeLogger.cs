using System;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests
{
    internal class FakeLoggerFactory : ILoggerFactory
    {
        internal Mock<ILogger> Logger { get; } = new();

        public void Dispose() { }

        public ILogger CreateLogger(string categoryName) => Logger.Object;

        public void AddProvider(ILoggerProvider provider) { }
    }

    internal static class FakeLoggerExtensions
    {
        internal static void WasCalledWith(this Mock<ILogger> fakeLogger, LogLevel logLevel, Func<string, bool> messageExpectation, Exception ex = null)
        {
            fakeLogger.Verify(
                logger => logger.Log(
                    It.Is<LogLevel>(l => l == logLevel),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((message, _) => messageExpectation(message == null ? null : message.ToString())),
                    It.Is<Exception>(thrown => ex == null || ex == thrown),
                    It.Is<Func<It.IsAnyType, Exception, string>>((_, __) => true)));
        }
    }
}
