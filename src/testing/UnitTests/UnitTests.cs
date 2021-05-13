using System;
using Microsoft.Extensions.Logging;
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
        private readonly FakeLoggerFactory _fakeLoggerFactory = new();

        protected Mock<ILogger> FakeLogger => _fakeLoggerFactory.Logger;

        protected UnitTests()
        {
            LoggerManager.UseFactory(_fakeLoggerFactory);
        }

        public void Dispose()
        {
            LoggerManager.ResetToDefaults();
            _fakeLoggerFactory.Dispose();
            OnAfterEachTest();
            OnDisposing();
        }

        protected virtual void OnAfterEachTest() { }
        protected virtual void OnDisposing() { }
    }
}
