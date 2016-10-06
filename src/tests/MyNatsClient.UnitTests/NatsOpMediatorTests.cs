using System;
using System.Reactive;
using FluentAssertions;
using Moq;
using MyNatsClient.Ops;
using Xunit;

namespace MyNatsClient.UnitTests
{
    public class NatsOpMediatorTests : UnitTestsOf<NatsOpMediator>
    {
        public NatsOpMediatorTests()
        {
            UnitUnderTest = new NatsOpMediator();
        }

        [Fact]
        public void Dispatching_Should_update_date_time_for_last_received_op()
        {
            var op = Mock.Of<IOp>();
            UnitUnderTest.LastOpReceivedAt.Should().Be(DateTime.MinValue);

            UnitUnderTest.Dispatch(op);

            UnitUnderTest.LastOpReceivedAt.Should().BeCloseTo(DateTime.UtcNow);
        }

        [Fact]
        public void Dispatching_Should_update_op_count()
        {
            var op = Mock.Of<IOp>();

            UnitUnderTest.Dispatch(op);
            UnitUnderTest.Dispatch(op);

            UnitUnderTest.OpCount.Should().Be(2);
        }

        [Fact]
        public void Dispatching_MsgOp_Should_dispatch_to_both_OpStream_and_MsgOpStream()
        {
            var msgOp = new MsgOp("TestSubject", "0a3282e769e34677809db5d756dfd768", new byte[0]);
            var opStreamRec = false;
            var msgOpStreamRec = false;
            UnitUnderTest.Subscribe(new AnonymousObserver<IOp>(op => opStreamRec = true));
            UnitUnderTest.Subscribe(new AnonymousObserver<MsgOp>(op => msgOpStreamRec = true));

            UnitUnderTest.Dispatch(msgOp);

            opStreamRec.Should().BeTrue();
            msgOpStreamRec.Should().BeTrue();
        }

        [Fact]
        public void Dispatching_non_MsgOp_Should_not_dispatch_to_MsgOpStream()
        {
            var opStreamRec = false;
            var msgOpStreamRec = false;
            UnitUnderTest.Subscribe(new AnonymousObserver<IOp>(op => opStreamRec = true));
            UnitUnderTest.Subscribe(new AnonymousObserver<MsgOp>(op => msgOpStreamRec = true));

            UnitUnderTest.Dispatch(PingOp.Instance);

            opStreamRec.Should().BeTrue();
            msgOpStreamRec.Should().BeFalse();
        }
    }
}