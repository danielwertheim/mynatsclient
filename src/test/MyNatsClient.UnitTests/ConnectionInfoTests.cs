using FluentAssertions;
using NUnit.Framework;

namespace MyNatsClient.UnitTests
{
    public class ConnectionInfoTests : UnitTestsOf<ConnectionInfo>
    {
        protected override void OnBeforeEachTest()
        {
            UnitUnderTest = new ConnectionInfo(new[] { new Host("localhost", 4222) });
        }

        [Test]
        public void Defaults_Should_have_empty_credentials()
        {
            UnitUnderTest.Credentials.Should().Be(Credentials.Empty);
        }

        [Test]
        public void Defaults_Should_not_be_verbose()
        {
            UnitUnderTest.Verbose.Should().BeFalse();
        }

        [Test]
        public void Defaults_Should_not_auto_respond_to_pings()
        {
            UnitUnderTest.AutoRespondToPing.Should().BeTrue();
        }

        [Test]
        public void Clone_Should_clone_all_properties()
        {
            UnitUnderTest.Credentials = new Credentials("theuser", "thepass");

            var cloned = UnitUnderTest.Clone();

            cloned.ShouldBeEquivalentTo(UnitUnderTest);
        }
    }
}