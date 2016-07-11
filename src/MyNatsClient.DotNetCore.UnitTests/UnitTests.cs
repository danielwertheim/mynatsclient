using System;
using NUnit.Framework;

namespace MyNatsClient.UnitTests
{
    [TestFixture]
    public abstract class UnitTestsOf<T> : UnitTests
    {
        protected T UnitUnderTest { get; set; }

        [TearDown]
        protected void Clean()
        {
            (UnitUnderTest as IDisposable)?.Dispose();
        }
    }

    [TestFixture]
    public abstract class UnitTests
    {
        [SetUp]
        protected virtual void OnBeforeEachTest() { }

        [TearDown]
        protected virtual void OnAfterEachTest() { }

        [OneTimeSetUp]
        protected virtual void OnBeforeAllTests() { }

        [OneTimeTearDown]
        protected virtual void OnAfterAllTests() { }
    }
}