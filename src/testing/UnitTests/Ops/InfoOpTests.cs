using System;
using FluentAssertions;
using MyNatsClient.Ops;
using Xunit;

namespace UnitTests.Ops
{
    public class InfoOpTests : UnitTestsOf<InfoOp>
    {
        [Fact]
        public void Is_initialized_properly()
        {
            UnitUnderTest = new InfoOp("Foo Bar".AsSpan());

            UnitUnderTest.Marker.Should().Be("INFO");
            UnitUnderTest.Message.Span.ToString().Should().Be("Foo Bar");
            UnitUnderTest.ToString().Should().Be("INFO");
        }
    }
}
