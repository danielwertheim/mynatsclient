using System;
using System.Threading;
using FluentAssertions;
using Moq;
using MyNatsClient;
using Xunit;

namespace UnitTests
{
    public class SafeObservableOfTests : UnitTestsOf<SafeObservableOf<IClientEvent>>
    {
        public SafeObservableOfTests()
        {
            UnitUnderTest = new SafeObservableOf<IClientEvent>();
        }

        [Fact]
        public void Emitting_Should_not_fail_When_no_observers_exists()
        {
            Action a = () => UnitUnderTest.Emit(Mock.Of<IClientEvent>());

            a.ShouldNotThrow();
        }

        [Fact]
        public void Emitting_Should_dispatch_to_all_observers()
        {
            var callCount = 0;

            UnitUnderTest.Subscribe(ev => Interlocked.Increment(ref callCount));
            UnitUnderTest.Subscribe(ev => Interlocked.Increment(ref callCount));

            UnitUnderTest.Emit(Mock.Of<IClientEvent>());

            callCount.Should().Be(2);
        }

        [Fact]
        public void Emitting_Should_not_invoke_any_subscriptions_onError_When_exception_is_thrown()
        {
            var exHandlerWasInvoked = false;

            UnitUnderTest.Subscribe(
                ev => throw new Exception("I FAILED!"),
                ex => exHandlerWasInvoked = true);

            UnitUnderTest.Subscribe(
                ev => { },
                ex => exHandlerWasInvoked = true);

            UnitUnderTest.Emit(Mock.Of<IClientEvent>());

            exHandlerWasInvoked.Should().BeFalse();
        }

        [Fact]
        public void Emitting_Should_continue_emitting_to_failing_handler_and_other_handlers_When_a_handler_has_failed()
        {
            var nonFailingObserver = new Mock<IObserver<IClientEvent>>();
            var failingObserver = new Mock<IObserver<IClientEvent>>();
            failingObserver.Setup(f => f.OnNext(It.IsAny<IClientEvent>())).Throws<Exception>();

            UnitUnderTest.Subscribe(nonFailingObserver.Object);
            UnitUnderTest.Subscribe(failingObserver.Object);

            UnitUnderTest.Emit(Mock.Of<IClientEvent>());
            UnitUnderTest.Emit(Mock.Of<IClientEvent>());

            nonFailingObserver.Verify(f => f.OnNext(It.IsAny<IClientEvent>()), Times.Exactly(2));
            nonFailingObserver.Verify(f => f.OnError(It.IsAny<Exception>()), Times.Never);
            failingObserver.Verify(f => f.OnNext(It.IsAny<IClientEvent>()), Times.Exactly(2));
            failingObserver.Verify(f => f.OnError(It.IsAny<Exception>()), Times.Never);
        }

        [Fact]
        public void Emitting_Should_invoke_logger_for_error_When_exception_is_thrown()
        {
            var thrown = new Exception("I FAILED!");
            UnitUnderTest.Subscribe(msg => throw thrown);

            UnitUnderTest.Emit(Mock.Of<IClientEvent>());

            FakeLogger.Verify(f => f.Error("Error in observer while processing value.", thrown), Times.Once);
        }

        [Fact]
        public void Emitting_Should_not_dispatch_to_a_disposed_observer()
        {
            var s1CallCount = 0;
            var s2CallCount = 0;

            var s1 = UnitUnderTest.Subscribe(ev =>
            {
                Interlocked.Increment(ref s1CallCount);
            });
            var s2 = UnitUnderTest.Subscribe(ev =>
            {
                Interlocked.Increment(ref s2CallCount);
            });

            UnitUnderTest.Emit(Mock.Of<IClientEvent>());
            s1.Dispose();
            UnitUnderTest.Emit(Mock.Of<IClientEvent>());

            s1CallCount.Should().Be(1);
            s2CallCount.Should().Be(2);
        }
    }
}