using FluentAssertions;
using NUnit.Framework;

namespace MyNatsClient.UnitTests
{
    public class HostTests : UnitTestsOf<Host>
    {
        [Test]
        public void Should_have_default_port_4222()
        {
            new Host("host1").Port.Should().Be(4222);
        }

        [Test]
        [Combinatorial]
        public void Should_have_custom_ToString(
            [Values("host1", "host2")]string host,
            [Values(4222, 4223)]int port)
        {
            UnitUnderTest = new Host(host, port);

            UnitUnderTest.ToString().Should().Be($"{host}:{port}");
        }
    }
}