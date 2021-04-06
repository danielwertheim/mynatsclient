using FluentAssertions;
using MyNatsClient.Ops;
using Xunit;

namespace UnitTests.Ops
{
    public class PongOpTests : UnitTestsOf<PongOp>
    {
        [Fact]
        public void Is_initialized_properly()
        {
            UnitUnderTest = PongOp.Instance;

            UnitUnderTest.Marker.Should().Be("PONG");
            UnitUnderTest.ToString().Should().Be("PONG");
        }
    }
}
