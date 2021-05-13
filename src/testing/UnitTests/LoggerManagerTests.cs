using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MyNatsClient;
using Xunit;

namespace UnitTests
{
    public class LoggerManagerTests : UnitTests
    {
        [Fact]
        public void Should_return_NullLogger_by_default()
        {
            LoggerManager.ResetToDefaults();
            LoggerManager.CreateLogger(typeof(LoggerManagerTests)).Should().BeOfType<NullLogger>();
        }
    }
}
