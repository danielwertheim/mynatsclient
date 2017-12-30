using System;
using System.Reactive;
using System.Threading;
using FluentAssertions;
using Moq;
using MyNatsClient;
using Xunit;

namespace UnitTests
{
    public class ObservableOfTests : UnitTestsOf<ObservableOf<IClientEvent>>
    {
        public ObservableOfTests()
        {
            UnitUnderTest = new ObservableOf<IClientEvent>();
        }

        [Fact]
        public void Dispatching_Should_not_fail_When_no_observers_exists()
        {
            Action a = () => UnitUnderTest.Emit(Mock.Of<IClientEvent>());

            a.ShouldNotThrow();
        }

        [Fact]
        public void Dispatching_Should_dispatch_to_all_observers()
        {
            var callCount = 0;

            UnitUnderTest.Subscribe(ev => Interlocked.Increment(ref callCount));
            UnitUnderTest.Subscribe(ev => Interlocked.Increment(ref callCount));

            UnitUnderTest.Emit(Mock.Of<IClientEvent>());

            callCount.Should().Be(2);
        }

        [Fact]
        public void Dispatching_Should_invoke_subscriptions_onError_When_exception_is_thrown_by_observer()
        {
            var thrown = new Exception("I FAILED!");
            Exception caught = null;
            UnitUnderTest.Subscribe(
                ev => throw thrown,
                ex => caught = ex);

            UnitUnderTest.Emit(Mock.Of<IClientEvent>());

            caught.Should().NotBeNull();
            caught.Should().BeSameAs(thrown);
        }

        [Fact]
        public void Dispatching_Should_invoke_logger_for_error_When_exception_is_thrown_by_observer()
        {
            var thrown = new Exception("I FAILED!");
            UnitUnderTest.Subscribe(new AnonymousObserver<IClientEvent>(msg => throw thrown));

            UnitUnderTest.Emit(Mock.Of<IClientEvent>());

            FakeLogger.Verify(f => f.Error("Error in observer while processing message.", thrown), Times.Once);
        }

        [Fact]
        public void Dispatching_Should_invoke_onError_When_exception_is_thrown_by_observer()
        {
            var thrown = new Exception("I FAILED!");
            Exception caughtInCommonHandler = null;
            Exception caughtInObserverHandler = null;

            UnitUnderTest.OnException = (ev, ex) => caughtInCommonHandler = ex;

            UnitUnderTest.Subscribe(
                ev => throw thrown,
                ex => caughtInObserverHandler = ex);

            UnitUnderTest.Emit(Mock.Of<IClientEvent>());

            caughtInCommonHandler.Should().NotBeNull();
            caughtInCommonHandler.Should().BeSameAs(thrown);
            caughtInObserverHandler.Should().NotBeNull();
            caughtInObserverHandler.Should().BeSameAs(thrown);
        }

        [Fact]
        public void Dispatching_Should_not_dispatch_to_a_failed_observer()
        {
            UnitUnderTest = new ObservableOf<IClientEvent>();

            var fakeObserver = new Mock<IObserver<IClientEvent>>();
            fakeObserver.Setup(f => f.OnNext(It.IsAny<IClientEvent>())).Throws<Exception>();
            UnitUnderTest.Subscribe(fakeObserver.Object);

            UnitUnderTest.Emit(Mock.Of<IClientEvent>());
            UnitUnderTest.Emit(Mock.Of<IClientEvent>());

            fakeObserver.Verify(f => f.OnNext(It.IsAny<IClientEvent>()), Times.Exactly(1));
            fakeObserver.Verify(f => f.OnError(It.IsAny<Exception>()), Times.Exactly(1));
        }

        [Fact]
        public void Dispatching_Should_dispatch_to_non_failed_observer_When_a_failed_observer_exists()
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
        }

        [Fact]
        public void Dispatching_Should_not_dispatch_to_a_disposed_observer()
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