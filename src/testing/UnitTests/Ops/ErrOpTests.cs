using FluentAssertions;
using MyNatsClient.Ops;
using Xunit;

namespace UnitTests.Ops
{
    public class ErrOpTests : UnitTestsOf<ErrOp>
    {
        [Fact]
        public void Is_initialized_properly()
        {
            UnitUnderTest = new ErrOp("Foo Bar");

            UnitUnderTest.Marker.Should().Be("-ERR");
            UnitUnderTest.Message.Should().Be("Foo Bar");
            UnitUnderTest.ToString().Should().Be("-ERR");
        }
    }
}
