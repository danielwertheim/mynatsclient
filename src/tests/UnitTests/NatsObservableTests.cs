using System;
using System.Threading;
using FluentAssertions;
using Moq;
using MyNatsClient;
using MyNatsClient.Extensions;
using Xunit;

namespace UnitTests
{
    public class ExtendedData : Data
    {
        public virtual int OtherValue { get; set; }
    }

    public class Data
    {
        public virtual int Value { get; set; }
    }

    public class NatsObservableTests : UnitTestsOf<NatsObservableOf<Data>>
    {
        public NatsObservableTests()
        {
            UnitUnderTest = new NatsObservableOf<Data>();
        }

        [Fact]
        public void Emitting_Should_not_fail_When_no_observers_exists()
        {
            Action a = () => UnitUnderTest.Emit(Mock.Of<Data>());

            a.Should().NotThrow();
        }

        [Fact]
        public void Emitting_Should_dispatch_to_all_observers()
        {
            var callCount = 0;

            UnitUnderTest.Subscribe(ev => Interlocked.Increment(ref callCount));
            UnitUnderTest.Subscribe(ev => Interlocked.Increment(ref callCount));

            UnitUnderTest.Emit(Mock.Of<Data>());

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

            UnitUnderTest.Emit(Mock.Of<Data>());

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

            UnitUnderTest.Emit(Mock.Of<Data>());

            failingExHandlerWasInvoked.Should().BeFalse();
            nonFailingExHandlerWasInvoked.Should().BeFalse();
        }

        [Fact]
        public void Emitting_Should_not_continue_emitting_to_failing_observer_but_to_other_observers_When_an_observer_has_failed()
        {
            var failingObserver = new Mock<IObserver<Data>>();
            failingObserver.Setup(f => f.OnNext(It.IsAny<Data>())).Throws<Exception>();
            var nonFailingObserver = new Mock<IObserver<Data>>();

            UnitUnderTest.Subscribe(failingObserver.Object);
            UnitUnderTest.Subscribe(nonFailingObserver.Object);

            UnitUnderTest.Emit(Mock.Of<Data>());
            UnitUnderTest.Emit(Mock.Of<Data>());

            failingObserver.Verify(f => f.OnNext(It.IsAny<Data>()), Times.Once);
            failingObserver.Verify(f => f.OnError(It.IsAny<Exception>()), Times.Once);
            nonFailingObserver.Verify(f => f.OnNext(It.IsAny<Data>()), Times.Exactly(2));
            nonFailingObserver.Verify(f => f.OnError(It.IsAny<Exception>()), Times.Never);
        }

        [Fact]
        public void Emitting_Should_continue_emitting_to_failing_observer_and_other_observers_When_a_safe_observer_has_failed()
        {
            var failingObserver = new Mock<IObserver<Data>>();
            failingObserver.Setup(f => f.OnNext(It.IsAny<Data>())).Throws<Exception>();
            var nonFailingObserver = new Mock<IObserver<Data>>();

            UnitUnderTest.SubscribeSafe(failingObserver.Object);
            UnitUnderTest.Subscribe(nonFailingObserver.Object);

            UnitUnderTest.Emit(Mock.Of<Data>());
            UnitUnderTest.Emit(Mock.Of<Data>());

            failingObserver.Verify(f => f.OnNext(It.IsAny<Data>()), Times.Exactly(2));
            failingObserver.Verify(f => f.OnError(It.IsAny<Exception>()), Times.Never);
            nonFailingObserver.Verify(f => f.OnNext(It.IsAny<Data>()), Times.Exactly(2));
            nonFailingObserver.Verify(f => f.OnError(It.IsAny<Exception>()), Times.Never);
        }

        [Fact]
        public void Emitting_Should_not_continue_emitting_to_failing_observer_but_to_other_observers_When_an_observer_has_failed_in_error_handler()
        {
            var failingObserver = new Mock<IObserver<Data>>();
            failingObserver.Setup(f => f.OnNext(It.IsAny<Data>())).Throws<Exception>();
            failingObserver.Setup(f => f.OnError(It.IsAny<Exception>())).Throws<Exception>();
            var nonFailingObserver = new Mock<IObserver<Data>>();

            UnitUnderTest.Subscribe(failingObserver.Object);
            UnitUnderTest.Subscribe(nonFailingObserver.Object);

            UnitUnderTest.Emit(Mock.Of<Data>());
            UnitUnderTest.Emit(Mock.Of<Data>());

            failingObserver.Verify(f => f.OnNext(It.IsAny<Data>()), Times.Once);
            failingObserver.Verify(f => f.OnError(It.IsAny<Exception>()), Times.Once);
            nonFailingObserver.Verify(f => f.OnNext(It.IsAny<Data>()), Times.Exactly(2));
            nonFailingObserver.Verify(f => f.OnError(It.IsAny<Exception>()), Times.Never);
        }

        [Fact]
        public void Emitting_Should_invoke_logger_for_error_When_exception_is_thrown()
        {
            var thrown = new Exception("I FAILED!");
            UnitUnderTest.Subscribe(msg => throw thrown);

            UnitUnderTest.Emit(Mock.Of<Data>());

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

            UnitUnderTest.Emit(Mock.Of<Data>());
            s1.Dispose();
            UnitUnderTest.Emit(Mock.Of<Data>());

            s1CallCount.Should().Be(1);
            s2CallCount.Should().Be(2);
        }

        [Fact]
        public void Should_be_able_to_apply_predicate()
        {
            var observer1 = new Mock<IObserver<Data>>();
            var observer2 = new Mock<IObserver<Data>>();

            UnitUnderTest.Where(d => d.Value <= 2).Subscribe(observer1.Object);
            UnitUnderTest.Where(d => d.Value >= 2).Subscribe(observer2.Object);

            UnitUnderTest.Emit(new Data { Value = 1 });
            UnitUnderTest.Emit(new Data { Value = 2 });
            UnitUnderTest.Emit(new Data { Value = 3 });

            observer1.Verify(f => f.OnNext(It.IsAny<Data>()), Times.Exactly(2));
            observer1.Verify(f => f.OnNext(It.Is<Data>(d => d.Value == 1)), Times.Once);
            observer1.Verify(f => f.OnNext(It.Is<Data>(d => d.Value == 2)), Times.Once);

            observer2.Verify(f => f.OnNext(It.IsAny<Data>()), Times.Exactly(2));
            observer2.Verify(f => f.OnNext(It.Is<Data>(d => d.Value == 2)), Times.Once);
            observer2.Verify(f => f.OnNext(It.Is<Data>(d => d.Value == 3)), Times.Once);
        }

        [Fact]
        public void Should_be_able_to_cast()
        {
            var observer1 = new Mock<IObserver<Data>>();
            var observer2 = new Mock<IObserver<ExtendedData>>();

            UnitUnderTest.Cast<Data, Data>().Subscribe(observer1.Object);
            UnitUnderTest.Cast<Data, ExtendedData>().Subscribe(observer2.Object);

            UnitUnderTest.Emit(new ExtendedData { Value = 1, OtherValue = 2});

            observer1.Verify(f => f.OnNext(It.IsAny<Data>()), Times.Exactly(1));
            observer1.Verify(f => f.OnNext(It.Is<Data>(d => d.Value == 1)), Times.Once);

            observer2.Verify(f => f.OnNext(It.IsAny<ExtendedData>()), Times.Exactly(1));
            observer2.Verify(f => f.OnNext(It.Is<ExtendedData>(d => d.Value == 1 && d.OtherValue == 2)), Times.Once);
        }

        [Fact]
        public void Should_be_able_filter_by_type()
        {
            var observer1 = new Mock<IObserver<Data>>();
            var observer2 = new Mock<IObserver<ExtendedData>>();

            UnitUnderTest.OfType<ExtendedData>().Subscribe(observer1.Object);
            UnitUnderTest.OfType<ExtendedData>().Subscribe(observer2.Object);

            UnitUnderTest.Emit(new ExtendedData { Value = 1, OtherValue = 2});

            observer1.Verify(f => f.OnNext(It.IsAny<ExtendedData>()), Times.Exactly(1));
            observer1.Verify(f => f.OnNext(It.Is<ExtendedData>(d => d.Value == 1 && d.OtherValue == 2)), Times.Once);

            observer2.Verify(f => f.OnNext(It.IsAny<ExtendedData>()), Times.Exactly(1));
            observer2.Verify(f => f.OnNext(It.Is<ExtendedData>(d => d.Value == 1 && d.OtherValue == 2)), Times.Once);
        }
    }
}