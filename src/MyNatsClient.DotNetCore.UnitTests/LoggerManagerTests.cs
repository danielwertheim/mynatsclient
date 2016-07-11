using FluentAssertions;
using MyNatsClient.Logging;
using NUnit.Framework;

namespace MyNatsClient.UnitTests
{
    public class LoggerManagerTests : UnitTests
    {
        [Test]
        public void Should_return_NullLogger_by_default()
        {
            LoggerManager.Resolve(typeof(Fake1)).Should().BeOfType<NullLogger>();
            LoggerManager.Resolve(typeof(Fake2)).Should().BeOfType<NullLogger>();
        }

        private class Fake1 { }
        private class Fake2 { }
    }
}