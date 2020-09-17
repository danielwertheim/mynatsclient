using System;
using FluentAssertions;
using MyNatsClient;
using MyNatsClient.Ops;
using MyNatsClient.Rx;
using Xunit;

namespace UnitTests
{
    public class NatsOpMediatorTests : UnitTestsOf<NatsOpMediator>
    {
        public NatsOpMediatorTests()
        {
            UnitUnderTest = new NatsOpMediator();
        }

        [Fact]
        public void Emitting_MsgOp_Should_dispatch_to_both_AllOpsStream_and_MsgOpsStream()
        {
            var msgOp = new MsgOp("TestSubject", "0a3282e769e34677809db5d756dfd768", ReadOnlySpan<char>.Empty, new byte[0]);
            var opStreamRec = false;
            var msgOpStreamRec = false;
            UnitUnderTest.AllOpsStream.Subscribe(NatsObserver.Delegating<IOp>(op => opStreamRec = true));
            UnitUnderTest.MsgOpsStream.Subscribe(NatsObserver.Delegating<MsgOp>(op => msgOpStreamRec = true));

            UnitUnderTest.Emit(msgOp);

            opStreamRec.Should().BeTrue();
            msgOpStreamRec.Should().BeTrue();
        }

        [Fact]
        public void Emitting_non_MsgOp_Should_not_dispatch_to_MsgOpsStream_but_AllOpsStream()
        {
            var opStreamRec = false;
            var msgOpStreamRec = false;
            UnitUnderTest.AllOpsStream.Subscribe(NatsObserver.Delegating<IOp>(op => opStreamRec = true));
            UnitUnderTest.MsgOpsStream.Subscribe(NatsObserver.Delegating<MsgOp>(op => msgOpStreamRec = true));

            UnitUnderTest.Emit(PingOp.Instance);

            opStreamRec.Should().BeTrue();
            msgOpStreamRec.Should().BeFalse();
        }

        [Fact]
        public void Emitting_non_MsgOp_Should_continue_Emitting_When_using_observer_with_error_handler_but_failing_observer_gets_discarded()
        {
            var countA = 0;
            var countB = 0;
            var countC = 0;
            var exToThrow = new Exception(Guid.NewGuid().ToString());
            Exception caughtEx = null;

            UnitUnderTest.AllOpsStream.Subscribe(NatsObserver.Delegating<IOp>(op =>
            {
                if (countA == 0)
                {
                    countA += 1;
                    throw exToThrow;
                }

                countA += 1;
            }, ex => caughtEx = ex));
            UnitUnderTest.AllOpsStream.Subscribe(NatsObserver.Delegating<IOp>(op => countB += 1));
            UnitUnderTest.AllOpsStream.Subscribe(NatsObserver.Delegating<IOp>(op => countC += 1));

            UnitUnderTest.Emit(PingOp.Instance);
            UnitUnderTest.Emit(PingOp.Instance);

            caughtEx.Should().BeNull();
            countA.Should().Be(1);
            countB.Should().Be(2);
            countC.Should().Be(2);
        }

        [Fact]
        public void Emitting_non_MsgOp_Should_continue_Emitting_When_using_delegate_with_error_handler_but_failing_observer_gets_discarded()
        {
            var countA = 0;
            var countB = 0;
            var countC = 0;
            var exToThrow = new Exception(Guid.NewGuid().ToString());
            Exception caughtEx = null;

            UnitUnderTest.AllOpsStream.Subscribe(op =>
            {
                if (countA == 0)
                {
                    countA += 1;
                    throw exToThrow;
                }

                countA += 1;
            }, ex => caughtEx = ex);
            UnitUnderTest.AllOpsStream.Subscribe(op => countB += 1);
            UnitUnderTest.AllOpsStream.Subscribe(op => countC += 1);

            UnitUnderTest.Emit(PingOp.Instance);
            UnitUnderTest.Emit(PingOp.Instance);

            caughtEx.Should().BeNull();
            countA.Should().Be(1);
            countB.Should().Be(2);
            countC.Should().Be(2);
        }

        [Fact]
        public void Emitting_non_MsgOp_Should_continue_Emitting_When_using_observer_without_error_handler_but_failing_observer_gets_discarded()
        {
            var countA = 0;
            var countB = 0;
            var countC = 0;

            UnitUnderTest.AllOpsStream.Subscribe(NatsObserver.Delegating<IOp>(op =>
            {
                if (countA == 0)
                {
                    countA += 1;
                    throw new Exception("Fail");
                }

                countA += 1;
            }));
            UnitUnderTest.AllOpsStream.Subscribe(NatsObserver.Delegating<IOp>(op => countB += 1));
            UnitUnderTest.AllOpsStream.Subscribe(NatsObserver.Delegating<IOp>(op => countC += 1));

            UnitUnderTest.Emit(PingOp.Instance);
            UnitUnderTest.Emit(PingOp.Instance);

            countA.Should().Be(1);
            countB.Should().Be(2);
            countC.Should().Be(2);
        }

        [Fact]
        public void Emitting_non_MsgOp_Should_continue_Emitting_When_using_delegate_without_error_handler_but_failing_observer_gets_discarded()
        {
            var countA = 0;
            var countB = 0;
            var countC = 0;

            UnitUnderTest.AllOpsStream.Subscribe(op =>
            {
                if (countA == 0)
                {
                    countA += 1;
                    throw new Exception("Fail");
                }

                countA += 1;
            });
            UnitUnderTest.AllOpsStream.Subscribe(op => countB += 1);
            UnitUnderTest.AllOpsStream.Subscribe(op => countC += 1);

            UnitUnderTest.Emit(PingOp.Instance);
            UnitUnderTest.Emit(PingOp.Instance);

            countA.Should().Be(1);
            countB.Should().Be(2);
            countC.Should().Be(2);
        }

        [Fact]
        public void Emitting_MsgOp_Should_continue_Emitting_When_using_observer_with_error_handler_but_failing_observer_gets_discarded()
        {
            var msgOp = new MsgOp("TestSubject", "f0dd86b9c2804632919b7b78292435e6", ReadOnlySpan<char>.Empty, new byte[0]);
            var countA = 0;
            var countB = 0;
            var countC = 0;
            var exToThrow = new Exception(Guid.NewGuid().ToString());
            Exception caughtEx = null;

            UnitUnderTest.MsgOpsStream.Subscribe(NatsObserver.Delegating<IOp>(op =>
            {
                if (countA == 0)
                {
                    countA += 1;
                    throw exToThrow;
                }

                countA += 1;
            }, ex => caughtEx = ex));
            UnitUnderTest.MsgOpsStream.Subscribe(NatsObserver.Delegating<IOp>(op => countB += 1));
            UnitUnderTest.MsgOpsStream.Subscribe(NatsObserver.Delegating<IOp>(op => countC += 1));

            UnitUnderTest.Emit(msgOp);
            UnitUnderTest.Emit(msgOp);

            caughtEx.Should().BeNull();
            countA.Should().Be(1);
            countB.Should().Be(2);
            countC.Should().Be(2);
        }

        [Fact]
        public void Emitting_MsgOp_Should_continue_Emitting_When_using_delegate_with_error_handler_but_failing_observer_gets_discarded()
        {
            var msgOp = new MsgOp("TestSubject", "01c549bed5f643e484c2841aff7a0d9d", ReadOnlySpan<char>.Empty, new byte[0]);
            var countA = 0;
            var countB = 0;
            var countC = 0;
            var exToThrow = new Exception(Guid.NewGuid().ToString());
            Exception caughtEx = null;

            UnitUnderTest.MsgOpsStream.Subscribe(op =>
            {
                if (countA == 0)
                {
                    countA += 1;
                    throw exToThrow;
                }

                countA += 1;
            }, ex => caughtEx = ex);
            UnitUnderTest.MsgOpsStream.Subscribe(op => countB += 1);
            UnitUnderTest.MsgOpsStream.Subscribe(op => countC += 1);

            UnitUnderTest.Emit(msgOp);
            UnitUnderTest.Emit(msgOp);

            caughtEx.Should().BeNull();
            countA.Should().Be(1);
            countB.Should().Be(2);
            countC.Should().Be(2);
        }

        [Fact]
        public void Emitting_MsgOp_Should_continue_Emitting_When_using_observer_without_error_handler_but_failing_observer_gets_discarded()
        {
            var msgOp = new MsgOp("TestSubject", "60a152d4b5804b23abe088eeac63b55e", ReadOnlySpan<char>.Empty, new byte[0]);
            var countA = 0;
            var countB = 0;
            var countC = 0;

            UnitUnderTest.MsgOpsStream.Subscribe(NatsObserver.Delegating<IOp>(op =>
            {
                if (countA == 0)
                {
                    countA += 1;
                    throw new Exception("Fail");
                }

                countA += 1;
            }));
            UnitUnderTest.MsgOpsStream.Subscribe(NatsObserver.Delegating<IOp>(op => countB += 1));
            UnitUnderTest.MsgOpsStream.Subscribe(NatsObserver.Delegating<IOp>(op => countC += 1));

            UnitUnderTest.Emit(msgOp);
            UnitUnderTest.Emit(msgOp);

            countA.Should().Be(1);
            countB.Should().Be(2);
            countC.Should().Be(2);
        }

        [Fact]
        public void Emitting_MsgOp_Should_continue_Emitting_When_using_delegate_without_error_handler_but_failing_observer_gets_discarded()
        {
            var msgOp = new MsgOp("TestSubject", "e8fb57beeb094bbfb545056057a8f7f2", ReadOnlySpan<char>.Empty, new byte[0]);
            var countA = 0;
            var countB = 0;
            var countC = 0;

            UnitUnderTest.MsgOpsStream.Subscribe(op =>
            {
                if (countA == 0)
                {
                    countA += 1;
                    throw new Exception("Fail");
                }

                countA += 1;
            });
            UnitUnderTest.MsgOpsStream.Subscribe(op => countB += 1);
            UnitUnderTest.MsgOpsStream.Subscribe(op => countC += 1);

            UnitUnderTest.Emit(msgOp);
            UnitUnderTest.Emit(msgOp);

            countA.Should().Be(1);
            countB.Should().Be(2);
            countC.Should().Be(2);
        }
    }
}