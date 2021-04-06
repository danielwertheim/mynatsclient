using FluentAssertions;
using MyNatsClient.Ops;
using Xunit;

namespace UnitTests.Ops
{
    public class OkOpTests : UnitTestsOf<OkOp>
    {
        [Fact]
        public void Is_initialized_properly()
        {
            UnitUnderTest = OkOp.Instance;

            UnitUnderTest.Marker.Should().Be("+OK");
            UnitUnderTest.ToString().Should().Be("+OK");
        }
    }
}
