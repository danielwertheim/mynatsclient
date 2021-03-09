﻿using FluentAssertions;
using MyNatsClient;
using MyNatsClient.Logging;
using Xunit;

namespace UnitTests
{
    public class LoggerManagerTests : UnitTests
    {
        [Fact]
        public void Should_return_NullLogger_by_default()
        {
            LoggerManager.ResetToDefaults();
            LoggerManager.Resolve(typeof(Fake1)).Should().BeOfType<NullLogger>();
        }

        private class Fake1 { }
        private class Fake2 { }
    }
}