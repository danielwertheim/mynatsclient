using FluentAssertions;
using MyNatsClient;
using Xunit;

namespace UnitTests
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
        public void Defaults_Should_have_auto_reconnect_on_failure()
        {
            UnitUnderTest.AutoReconnectOnFailure.Should().BeTrue();
        }

        [Fact]
        public void Defaults_Should_auto_respond_to_pings()
        {
            UnitUnderTest.AutoRespondToPing.Should().BeTrue();
        }

        [Fact]
        public void Defaults_Should_have_auto_pub_flush_mode()
        {
            UnitUnderTest.PubFlushMode.Should().Be(PubFlushMode.Auto);
        }

        [Fact]
        public void Defaults_Should_have_a_request_timeout_of_5s()
        {
            UnitUnderTest.RequestTimeoutMs.Should().Be(5000);
        }

        [Fact]
        public void Defaults_Should_have_a_socket_recieve_timeout_of_5s()
        {
            UnitUnderTest.SocketOptions.ReceiveTimeoutMs.Should().Be(5000);
        }

        [Fact]
        public void Defaults_Should_have_a_socket_send_timeout_of_5s()
        {
            UnitUnderTest.SocketOptions.SendTimeoutMs.Should().Be(5000);
        }

        [Fact]
        public void Defaults_Should_have_a_socket_connect_timeout_of_5s()
        {
            UnitUnderTest.SocketOptions.ConnectTimeoutMs.Should().Be(5000);
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
                RequestTimeoutMs = 100,
                PubFlushMode = UnitUnderTest.PubFlushMode,
                SocketOptions = new SocketOptions
                {
                    ReceiveBufferSize = 1000,
                    ReceiveTimeoutMs = 100,
                    SendBufferSize = 1000,
                    SendTimeoutMs = 100,
                    ConnectTimeoutMs = 100
                }
            };

            var cloned = other.Clone();

            cloned.ShouldBeEquivalentTo(other);
        }
    }
}