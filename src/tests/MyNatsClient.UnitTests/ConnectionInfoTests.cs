using FluentAssertions;
using Xunit;

namespace MyNatsClient.UnitTests
{
    public class ConnectionInfoTests : UnitTestsOf<ConnectionInfo>
    {
        public ConnectionInfoTests()
        {
            UnitUnderTest = new ConnectionInfo(new[] { new Host("localhost", 4222) });
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
        public void Defaults_Should_not_auto_respond_to_pings()
        {
            UnitUnderTest.AutoRespondToPing.Should().BeTrue();
        }

        [Fact]
        public void Clone_Should_clone_all_properties()
        {
            UnitUnderTest.Credentials = new Credentials("theuser", "thepass");

            var cloned = UnitUnderTest.Clone();

            cloned.ShouldBeEquivalentTo(UnitUnderTest);
        }
    }
}