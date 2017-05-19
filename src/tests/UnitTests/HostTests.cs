using FluentAssertions;
using MyNatsClient;
using Xunit;

namespace UnitTests
{
    public class HostTests : UnitTestsOf<Host>
    {
        [Fact]
        public void Should_have_default_port_4222()
        {
            UnitUnderTest = new Host("host1");
            UnitUnderTest.Port.Should().Be(4222);
        }

        [Theory]
        [InlineData("host1", 9999)]
        public void Should_have_custom_ToString(string host, int port)
        {
            UnitUnderTest = new Host(host, port);

            UnitUnderTest.ToString().Should().Be($"{host}:{port}");
        }
    }
}