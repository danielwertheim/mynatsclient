using FluentAssertions;
using Xunit;

namespace MyNatsClient.UnitTests
{
    public class ConnectionInfoTests : UnitTestsOf<ConnectionInfo>
    {
        public ConnectionInfoTests()
        {
            UnitUnderTest = new ConnectionInfo(new Host("localhost"));
        }

        [Fact]
        public void Defaults_Should_have_empty_credentials()
        {
            UnitUnderTest.Credentials.Should().Be(Credentials.Empty);
        }

        [Fact]
        public void Defaults_Should_not_be_verbose()
        {
            UnitUnderTest.Verbose.Should().BeFalse();
        }

        [Fact]
        public void Defaults_Should_not_have_auto_reconnect_on_failure()
        {
            UnitUnderTest.AutoReconnectOnFailure.Should().BeFalse();
        }

        [Fact]
        public void Defaults_Should_not_auto_respond_to_pings()
        {
            UnitUnderTest.AutoRespondToPing.Should().BeTrue();
        }

        [Fact]
        public void Clone_Should_clone_all_properties()
        {
            var other = new ConnectionInfo(new[] { new Host("192.168.1.20", 4223) })
            {
                Credentials = new Credentials("tester", "p@ssword"),
                Verbose = !UnitUnderTest.Verbose,
                AutoReconnectOnFailure = !UnitUnderTest.AutoReconnectOnFailure,
                AutoRespondToPing = !UnitUnderTest.AutoRespondToPing,
                PubFlushMode = UnitUnderTest.PubFlushMode,
                SocketOptions = new SocketOptions
                {
                    ReceiveBufferSize = 1000,
                    ReceiveTimeoutMs = 100,
                    SendBufferSize = 2000,
                    SendTimeoutMs = 200
                }
            };

            var cloned = other.Clone();

            cloned.ShouldBeEquivalentTo(other);
        }
    }
}