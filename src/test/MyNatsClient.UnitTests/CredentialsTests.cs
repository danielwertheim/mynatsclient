using FluentAssertions;
using NUnit.Framework;

namespace MyNatsClient.UnitTests
{
    public class CredentialsTests : UnitTestsOf<Credentials>
    {
        protected override void OnBeforeEachTest()
        {
            UnitUnderTest = new Credentials("theuser", "thepass");
        }

        [Test]
        public void Empty_Should_return_same_instance()
        {
            Credentials.Empty.Should().BeSameAs(Credentials.Empty);
        }

        [Test]
        public void Should_have_equals_operator()
        {
            (UnitUnderTest == new Credentials(UnitUnderTest.User, UnitUnderTest.Pass)).Should().BeTrue();

            (UnitUnderTest == new Credentials(UnitUnderTest.User + "a", UnitUnderTest.Pass)).Should().BeFalse();

            (UnitUnderTest == new Credentials(UnitUnderTest.User, UnitUnderTest.Pass + "a")).Should().BeFalse();

            (new Credentials("THEUSER", "THEPASS") == new Credentials("theuser", "THEPASS")).Should().BeFalse();

            (new Credentials("THEUSER", "THEPASS") == new Credentials("THEUSER", "thepass")).Should().BeFalse();
        }

        [Test]
        public void Should_have_not_equals_operator()
        {
            (UnitUnderTest != new Credentials(UnitUnderTest.User, UnitUnderTest.Pass)).Should().BeFalse();

            (UnitUnderTest != new Credentials(UnitUnderTest.User + "a", UnitUnderTest.Pass)).Should().BeTrue();

            (UnitUnderTest != new Credentials(UnitUnderTest.User, UnitUnderTest.Pass + "a")).Should().BeTrue();

            (new Credentials("THEUSER", "THEPASS") != new Credentials("theuser", "THEPASS")).Should().BeTrue();

            (new Credentials("THEUSER", "THEPASS") != new Credentials("THEUSER", "thepass")).Should().BeTrue();
        }
    }
}