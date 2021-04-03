using FluentAssertions;
using MyNatsClient.Ops;
using Xunit;

namespace UnitTests.Ops
{
    public class PingOpTests : UnitTestsOf<PingOp>
    {
        [Fact]
        public void Is_initialized_properly()
        {
            UnitUnderTest = PingOp.Instance;

            UnitUnderTest.Marker.Should().Be("PING");
            UnitUnderTest.ToString().Should().Be("PING");
        }
    }
}
