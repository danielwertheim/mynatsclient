using System;
using System.Threading;
using FluentAssertions;
using Moq;
using MyNatsClient;
using MyNatsClient.Extensions;
using Xunit;

namespace UnitTests
{
    public class NatsObservableTests : UnitTestsOf<NatsObservableOf<IClientEvent>>
    {
        public NatsObservableTests()
        {
            UnitUnderTest = new NatsObservableOf<IClientEvent>();
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
        public void Emitting_Should_only_invoke_onError_of_failing_observer_When_exception_is_thrown()
        {
            var failingExHandlerWasInvoked = false;
            var nonFailingExHandlerWasInvoked = false;

            UnitUnderTest.Subscribe(
                ev => throw new Exception("I FAILED!"),
                ex => failingExHandlerWasInvoked = true);

            UnitUnderTest.Subscribe(
                ev => { },
                ex => nonFailingExHandlerWasInvoked = true);

            UnitUnderTest.Emit(Mock.Of<IClientEvent>());

            failingExHandlerWasInvoked.Should().BeTrue();
            nonFailingExHandlerWasInvoked.Should().BeFalse();
        }

        [Fact]
        public void Emitting_Should_not_invoke_onError_on_any_observer_When_exception_is_thrown_in_safe_observer()
        {
            var failingExHandlerWasInvoked = false;
            var nonFailingExHandlerWasInvoked = false;

            UnitUnderTest.SubscribeSafe(
                ev => throw new Exception("I FAILED!"),
                ex => failingExHandlerWasInvoked = true);

            UnitUnderTest.Subscribe(
                ev => { },
                ex => nonFailingExHandlerWasInvoked = true);

            UnitUnderTest.Emit(Mock.Of<IClientEvent>());

            failingExHandlerWasInvoked.Should().BeFalse();
            nonFailingExHandlerWasInvoked.Should().BeFalse();
        }

        [Fact]
        public void Emitting_Should_not_continue_emitting_to_failing_observer_but_to_other_observers_When_an_observer_has_failed()
        {
            var failingObserver = new Mock<IObserver<IClientEvent>>();
            failingObserver.Setup(f => f.OnNext(It.IsAny<IClientEvent>())).Throws<Exception>();
            var nonFailingObserver = new Mock<IObserver<IClientEvent>>();

            UnitUnderTest.Subscribe(failingObserver.Object);
            UnitUnderTest.Subscribe(nonFailingObserver.Object);

            UnitUnderTest.Emit(Mock.Of<IClientEvent>());
            UnitUnderTest.Emit(Mock.Of<IClientEvent>());

            failingObserver.Verify(f => f.OnNext(It.IsAny<IClientEvent>()), Times.Once);
            failingObserver.Verify(f => f.OnError(It.IsAny<Exception>()), Times.Once);
            nonFailingObserver.Verify(f => f.OnNext(It.IsAny<IClientEvent>()), Times.Exactly(2));
            nonFailingObserver.Verify(f => f.OnError(It.IsAny<Exception>()), Times.Never);
        }

        [Fact]
        public void Emitting_Should_continue_emitting_to_failing_observer_and_other_observers_When_a_safe_observer_has_failed()
        {
            var failingObserver = new Mock<IObserver<IClientEvent>>();
            failingObserver.Setup(f => f.OnNext(It.IsAny<IClientEvent>())).Throws<Exception>();
            var nonFailingObserver = new Mock<IObserver<IClientEvent>>();

            UnitUnderTest.SubscribeSafe(failingObserver.Object);
            UnitUnderTest.Subscribe(nonFailingObserver.Object);

            UnitUnderTest.Emit(Mock.Of<IClientEvent>());
            UnitUnderTest.Emit(Mock.Of<IClientEvent>());

            failingObserver.Verify(f => f.OnNext(It.IsAny<IClientEvent>()), Times.Exactly(2));
            failingObserver.Verify(f => f.OnError(It.IsAny<Exception>()), Times.Never);
            nonFailingObserver.Verify(f => f.OnNext(It.IsAny<IClientEvent>()), Times.Exactly(2));
            nonFailingObserver.Verify(f => f.OnError(It.IsAny<Exception>()), Times.Never);
        }

        [Fact]
        public void Emitting_Should_invoke_logger_for_error_When_exception_is_thrown()
        {
            var thrown = new Exception("I FAILED!");
            UnitUnderTest.Subscribe(msg => throw thrown);

            UnitUnderTest.Emit(Mock.Of<IClientEvent>());

            FakeLogger.Verify(f => f.Error("Error in observer while emitting value.", thrown), Times.Once);
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