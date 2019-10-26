using System;
using Moq;
using MyNatsClient;

namespace UnitTests
{
    public abstract class UnitTestsOf<T> : UnitTests
    {
        protected T UnitUnderTest { get; set; }

        protected override void OnDisposing()
            => (UnitUnderTest as IDisposable)?.Dispose();
    }

    public abstract class UnitTests : IDisposable
    {
        protected Mock<ILogger> FakeLogger { get; }

        protected UnitTests()
        {
            FakeLogger = new Mock<ILogger>();
            LoggerManager.Resolve = _ => FakeLogger.Object;
        }

        public void Dispose()
        {
            OnAfterEachTest();
            OnDisposing();
        }

        protected virtual void OnAfterEachTest() { }
        protected virtual void OnDisposing() { }
    }
}