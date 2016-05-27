using System;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace MyNatsClient.UnitTests
{
    public class NatsOpMediatorTests : UnitTestsOf<NatsOpMediator>
    {
        protected override void OnBeforeEachTest()
        {
            UnitUnderTest = new NatsOpMediator();
        }

        [Test]
        public void Dispatching_Should_update_date_time_for_last_received_op()
        {
            var op = Mock.Of<IOp>();
            UnitUnderTest.LastOpReceivedAt.Should().Be(DateTime.MinValue);

            UnitUnderTest.Dispatch(op);

            UnitUnderTest.LastOpReceivedAt.Should().BeCloseTo(DateTime.UtcNow);
        }

        [Test]
        public void Dispatching_Should_update_op_count()
        {
            var op = Mock.Of<IOp>();

            UnitUnderTest.Dispatch(op);
            UnitUnderTest.Dispatch(op);

            UnitUnderTest.OpCount.Should().Be(2);
        }
    }
}