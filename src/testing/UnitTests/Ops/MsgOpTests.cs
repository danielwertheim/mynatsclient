using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using MyNatsClient;
using MyNatsClient.Ops;
using Xunit;

namespace UnitTests.Ops
{
    public class MsgOpTests : UnitTestsOf<MsgOp>
    {
        [Fact]
        public void Is_initialized_properly_When_Msg()
        {
            UnitUnderTest = MsgOp.CreateMsg(
                "TestSub",
                "TestSubId",
                "TestReplyTo",
                Encoding.UTF8.GetBytes("TestPayload"));

            UnitUnderTest.Marker.Should().Be("MSG");
            UnitUnderTest.Subject.Should().Be("TestSub");
            UnitUnderTest.SubscriptionId.Should().Be("TestSubId");
            UnitUnderTest.ReplyTo.Should().Be("TestReplyTo");
            UnitUnderTest.Payload.ToArray().Should().BeEquivalentTo(Encoding.UTF8.GetBytes("TestPayload"));
            UnitUnderTest.GetPayloadAsString().Should().Be("TestPayload");
            UnitUnderTest.ToString().Should().Be("MSG");
        }

        [Fact]
        public void Is_initialized_properly_When_Msg_with_optionals_are_missing()
        {
            UnitUnderTest = MsgOp.CreateMsg(
                "TestSub",
                "TestSubId",
                ReadOnlySpan<char>.Empty,
                ReadOnlySpan<byte>.Empty);

            UnitUnderTest.Marker.Should().Be("MSG");
            UnitUnderTest.Subject.Should().Be("TestSub");
            UnitUnderTest.SubscriptionId.Should().Be("TestSubId");
            UnitUnderTest.ReplyTo.Should().BeEmpty();
            UnitUnderTest.Payload.IsEmpty.Should().BeTrue();
            UnitUnderTest.GetPayloadAsString().Should().BeEmpty();
            UnitUnderTest.ToString().Should().Be("MSG");
        }

        [Fact]
        public void Is_initialized_properly_When_HMsg()
        {
            UnitUnderTest = MsgOp.CreateHMsg(
                "TestSub",
                "TestSubId",
                "TestReplyTo",
                ReadOnlyMsgHeaders.Create("NATS/1.0", new Dictionary<string, IReadOnlyList<string>>
                {
                    {"Header1", new List<string>
                    {
                        "Value1.1"
                    }}
                }),
                Encoding.UTF8.GetBytes("TestPayload"));

            UnitUnderTest.Marker.Should().Be("HMSG");
            UnitUnderTest.Subject.Should().Be("TestSub");
            UnitUnderTest.SubscriptionId.Should().Be("TestSubId");
            UnitUnderTest.ReplyTo.Should().Be("TestReplyTo");
            UnitUnderTest.Headers.Should().HaveCount(1);
            UnitUnderTest.Payload.ToArray().Should().BeEquivalentTo(Encoding.UTF8.GetBytes("TestPayload"));
            UnitUnderTest.GetPayloadAsString().Should().Be("TestPayload");
            UnitUnderTest.ToString().Should().Be("HMSG");
        }

        [Fact]
        public void Is_initialized_properly_When_HMsg_with_optionals_are_missing()
        {
            UnitUnderTest = MsgOp.CreateHMsg(
                "TestSub",
                "TestSubId",
                ReadOnlySpan<char>.Empty,
                ReadOnlyMsgHeaders.Empty,
                ReadOnlySpan<byte>.Empty);

            UnitUnderTest.Marker.Should().Be("HMSG");
            UnitUnderTest.Subject.Should().Be("TestSub");
            UnitUnderTest.SubscriptionId.Should().Be("TestSubId");
            UnitUnderTest.ReplyTo.Should().BeEmpty();
            UnitUnderTest.Headers.Should().BeEmpty();
            UnitUnderTest.Payload.IsEmpty.Should().BeTrue();
            UnitUnderTest.GetPayloadAsString().Should().BeEmpty();
            UnitUnderTest.ToString().Should().Be("HMSG");
        }
    }
}
