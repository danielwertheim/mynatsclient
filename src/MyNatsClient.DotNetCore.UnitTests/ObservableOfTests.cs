﻿using System;
using System.Threading;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace MyNatsClient.UnitTests
{
    public class ObservableOfTests : UnitTestsOf<ObservableOf<IClientEvent>>
    {
        protected override void OnBeforeEachTest()
        {
            UnitUnderTest = new ObservableOf<IClientEvent>();
        }

        [Test]
        public void Dispatching_Should_not_fail_When_no_observers_exists()
        {
            Action a = () => UnitUnderTest.Dispatch(Mock.Of<IClientEvent>());

            a.ShouldNotThrow();
        }

        [Test]
        public void Dispatching_Should_dispatch_to_all_observers()
        {
            var callCount = 0;

            UnitUnderTest.Subscribe(ev =>
            {
                Interlocked.Increment(ref callCount);
            });
            UnitUnderTest.Subscribe(ev =>
            {
                Interlocked.Increment(ref callCount);
            });

            UnitUnderTest.Dispatch(Mock.Of<IClientEvent>());

            callCount.Should().Be(2);
        }

        [Test]
        public void Dispatching_Should_invoke_onError_When_exception_is_thrown_by_observer()
        {
            Exception thrown = new Exception("I FAILED!");
            Exception caught = null;
            UnitUnderTest.Subscribe(ev =>
            {
                throw thrown;
            }, ex => caught = ex);

            UnitUnderTest.Dispatch(Mock.Of<IClientEvent>());

            caught.Should().NotBeNull();
            caught.Should().BeSameAs(thrown);
        }

        [Test]
        public void Dispatching_Should_not_dispatch_to_a_failed_observer()
        {
            var fake = new Mock<IObserver<IClientEvent>>();
            fake.Setup(f => f.OnNext(It.IsAny<IClientEvent>())).Throws<Exception>();
            UnitUnderTest.Subscribe(fake.Object);

            UnitUnderTest.Dispatch(Mock.Of<IClientEvent>());
            UnitUnderTest.Dispatch(Mock.Of<IClientEvent>());

            fake.Verify(f => f.OnNext(It.IsAny<IClientEvent>()), Times.Once);
            fake.Verify(f => f.OnError(It.IsAny<Exception>()), Times.Once);
        }

        [Test]
        public void Dispatching_Should_dispatch_to_non_failed_observer_When_a_failed_observer_exists()
        {
            var nonFailingObserver = new Mock<IObserver<IClientEvent>>();
            var failingObserver = new Mock<IObserver<IClientEvent>>();
            failingObserver.Setup(f => f.OnNext(It.IsAny<IClientEvent>())).Throws<Exception>();

            UnitUnderTest.Subscribe(nonFailingObserver.Object);
            UnitUnderTest.Subscribe(failingObserver.Object);

            UnitUnderTest.Dispatch(Mock.Of<IClientEvent>());
            UnitUnderTest.Dispatch(Mock.Of<IClientEvent>());

            nonFailingObserver.Verify(f => f.OnNext(It.IsAny<IClientEvent>()), Times.Exactly(2));
            nonFailingObserver.Verify(f => f.OnError(It.IsAny<Exception>()), Times.Never);
            failingObserver.Verify(f => f.OnNext(It.IsAny<IClientEvent>()), Times.Once);
            failingObserver.Verify(f => f.OnError(It.IsAny<Exception>()), Times.Once);
        }

        [Test]
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

            UnitUnderTest.Dispatch(Mock.Of<IClientEvent>());
            s1.Dispose();
            UnitUnderTest.Dispatch(Mock.Of<IClientEvent>());

            s1CallCount.Should().Be(1);
            s2CallCount.Should().Be(2);
        }
    }
}