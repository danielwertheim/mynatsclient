using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using MyNatsClient;
using MyNatsClient.Ops;
using Xunit;

namespace UnitTests
{
    public class NatsOpStreamReaderTests : UnitTestsOf<NatsOpStreamReader>
    {
        [Fact]
        public void Should_be_able_to_parse_different_messages_as_a_stream()
        {
            using (var stream = CreateStream(
                "INFO {\"server_id\":\"H8RgvFtiq2zlQTA5dB0deh\"}\r\n",
                "+OK\r\n",
                "MSG foo siddw1 4\r\ntest\r\n",
                "MSG foo grp1 siddw1 4\r\ntest\r\n",
                "PING\r\n",
                "PONG\r\n",
                "-ERR 'Unknown Protocol Operation'\r\n"))
            {
                UnitUnderTest = NatsOpStreamReader.Use(stream);

                var infoOp = UnitUnderTest.ReadOp();
                infoOp.Should().BeOfType<InfoOp>();

                var okOp = UnitUnderTest.ReadOp();
                okOp.Should().BeOfType<OkOp>();

                var msgOp = UnitUnderTest.ReadOp();
                msgOp.Should().BeOfType<MsgOp>();

                var msgWithGroupOp = UnitUnderTest.ReadOp();
                msgWithGroupOp.Should().BeOfType<MsgOp>();

                var pingOp = UnitUnderTest.ReadOp();
                pingOp.Should().BeOfType<PingOp>();

                var pongOp = UnitUnderTest.ReadOp();
                pongOp.Should().BeOfType<PongOp>();

                var errOp = UnitUnderTest.ReadOp();
                errOp.Should().BeOfType<ErrOp>();

                UnitUnderTest.ReadOp().Should().Be(NullOp.Instance);
            }
        }

        [Fact]
        public void Should_be_able_to_parse_InfoOp()
        {
            using (var stream = CreateStream(
                "INFO {\"server_id\":\"H8RgvFtiq2zlQTA5dB0deh\"}\r\n"))
            {
                UnitUnderTest = NatsOpStreamReader.Use(stream);

                var op = UnitUnderTest.ReadOp().Should().BeOfType<InfoOp>().Subject;

                op.Marker.Should().Be("INFO");
                op.Message.ToString().Should().Be("{\"server_id\":\"H8RgvFtiq2zlQTA5dB0deh\"}");
            }
        }

        [Fact]
        public void Should_be_able_to_parse_OkOp()
        {
            using (var stream = CreateStream(
                "+OK\r\n"))
            {
                UnitUnderTest = NatsOpStreamReader.Use(stream);

                var op = UnitUnderTest.ReadOp().Should().BeOfType<OkOp>().Subject;

                op.Marker.Should().Be("+OK");
            }
        }

        [Fact]
        public void Should_be_able_to_parse_MsgOp()
        {
            using (var stream = CreateStream(
                "MSG Foo Siddw1 ReplyTo1 4\r\nTEST\r\n"))
            {
                UnitUnderTest = NatsOpStreamReader.Use(stream);

                var op = UnitUnderTest.ReadOp().Should().BeOfType<MsgOp>().Subject;

                op.Marker.Should().Be("MSG");
                op.Subject.Should().Be("Foo");
                op.SubscriptionId.Should().Be("Siddw1");
                op.ReplyTo.Should().Be("ReplyTo1");
                op.Payload.ToArray().Should().HaveCount(4);
                op.GetPayloadAsString().Should().Be("TEST");
            }
        }

        [Fact]
        public void Should_be_able_to_parse_MsgOp_When_optionals_are_missing()
        {
            using (var stream = CreateStream(
                "MSG Foo Siddw1 0\r\n\r\n"))
            {
                UnitUnderTest = NatsOpStreamReader.Use(stream);

                var op = UnitUnderTest.ReadOp().Should().BeOfType<MsgOp>().Subject;

                op.Marker.Should().Be("MSG");
                op.Subject.Should().Be("Foo");
                op.SubscriptionId.Should().Be("Siddw1");
                op.ReplyTo.Should().BeEmpty();
                op.Payload.ToArray().Should().BeEmpty();
                op.GetPayloadAsString().Should().BeEmpty();
            }
        }

        [Fact]
        public void Should_be_able_to_parse_MsgOp_When_message_has_new_line()
        {
            using (var stream = CreateStream(
                "MSG Foo Siddw1 ReplyTo1 6\r\nTE\r\nST\r\n"))
            {
                UnitUnderTest = NatsOpStreamReader.Use(stream);

                var op = UnitUnderTest.ReadOp().Should().BeOfType<MsgOp>().Subject;

                op.Marker.Should().Be("MSG");
                op.Subject.Should().Be("Foo");
                op.SubscriptionId.Should().Be("Siddw1");
                op.ReplyTo.Should().Be("ReplyTo1");
                op.Payload.ToArray().Should().HaveCount(6);
                op.GetPayloadAsString().Should().Be("TE\r\nST");
            }
        }

        [Fact]
        public void Should_be_able_to_parse_MsgOp_When_message_has_tab()
        {
            using (var stream = CreateStream(
                "MSG Foo Siddw1 ReplyTo1 5\r\nTE\tST\r\n"))
            {
                UnitUnderTest = NatsOpStreamReader.Use(stream);

                var op = UnitUnderTest.ReadOp().Should().BeOfType<MsgOp>().Subject;

                op.Marker.Should().Be("MSG");
                op.Subject.Should().Be("Foo");
                op.SubscriptionId.Should().Be("Siddw1");
                op.ReplyTo.Should().Be("ReplyTo1");
                op.Payload.ToArray().Should().HaveCount(5);
                op.GetPayloadAsString().Should().Be("TE\tST");
            }
        }

        [Fact]
        public void Should_be_able_to_parse_MsgOp_When_message_has_new_line_and_tab()
        {
            using (var stream = CreateStream(
                "MSG Foo Siddw1 ReplyTo1 7\r\nTE\tS\r\nT\r\n"))
            {
                UnitUnderTest = NatsOpStreamReader.Use(stream);

                var op = UnitUnderTest.ReadOp().Should().BeOfType<MsgOp>().Subject;

                op.Marker.Should().Be("MSG");
                op.Subject.Should().Be("Foo");
                op.SubscriptionId.Should().Be("Siddw1");
                op.ReplyTo.Should().Be("ReplyTo1");
                op.Payload.ToArray().Should().HaveCount(7);
                op.GetPayloadAsString().Should().Be("TE\tS\r\nT");
            }
        }

        [Fact]
        public void Should_be_able_to_parse_MsgOp_When_it_is_tab_delimited_instead_of_space()
        {
            using (var stream = CreateStream(
                "MSG\tFoo\tSiddw1\tReplyTo1\t4\r\nTEST\r\n"))
            {
                UnitUnderTest = NatsOpStreamReader.Use(stream);

                var op = UnitUnderTest.ReadOp().Should().BeOfType<MsgOp>().Subject;

                op.Marker.Should().Be("MSG");
                op.Subject.Should().Be("Foo");
                op.SubscriptionId.Should().Be("Siddw1");
                op.ReplyTo.Should().Be("ReplyTo1");
                op.Payload.ToArray().Should().HaveCount(4);
                op.GetPayloadAsString().Should().Be("TEST");
            }
        }

        [Fact]
        public void Should_be_able_to_parse_MsgOp_When_headers_are_defined()
        {
            using (var stream = CreateStream(
                "HMSG Foo Siddw1 ReplyTo1 66 72 NATS/1.0\r\nHeader1:Value1.1\r\nHeader2:Value2.1\r\nHeader2:Value2.2\r\n\r\nTE\r\nST\r\n"))
            {
                UnitUnderTest = NatsOpStreamReader.Use(stream);

                var op = UnitUnderTest.ReadOp().Should().BeOfType<MsgOp>().Subject;

                op.Marker.Should().Be("HMSG");
                op.Subject.Should().Be("Foo");
                op.SubscriptionId.Should().Be("Siddw1");
                op.ReplyTo.Should().Be("ReplyTo1");
                op.Headers.Protocol.Should().Be("NATS/1.0");
                op.Headers.Count.Should().Be(2);
                op.Headers["Header1"].Should().BeEquivalentTo("Value1.1");
                op.Headers["Header2"].Should().BeEquivalentTo("Value2.1", "Value2.2");
                op.Payload.ToArray().Should().HaveCount(6);
                op.GetPayloadAsString().Should().Be("TE\r\nST");
            }
        }

        [Fact]
        public void Should_be_able_to_parse_MsgOp_When_headers_are_defined_but_optionals_are_missing()
        {
            using (var stream = CreateStream(
                "HMSG Foo Siddw1 66 66 NATS/1.0\r\nHeader1:Value1.1\r\nHeader2:Value2.1\r\nHeader2:Value2.2\r\n\r\n\r\n"))
            {
                UnitUnderTest = NatsOpStreamReader.Use(stream);

                var op = UnitUnderTest.ReadOp().Should().BeOfType<MsgOp>().Subject;

                op.Marker.Should().Be("HMSG");
                op.Subject.Should().Be("Foo");
                op.SubscriptionId.Should().Be("Siddw1");
                op.ReplyTo.Should().BeEmpty();
                op.Headers.Count.Should().Be(2);
                op.Headers["Header1"].Should().BeEquivalentTo("Value1.1");
                op.Headers["Header2"].Should().BeEquivalentTo("Value2.1", "Value2.2");
                op.Payload.ToArray().Should().BeEmpty();
                op.GetPayloadAsString().Should().BeEmpty();
            }
        }

        [Fact]
        public void Should_be_able_to_parse_PingOp()
        {
            using (var stream = CreateStream(
                "PING\r\n"))
            {
                UnitUnderTest = NatsOpStreamReader.Use(stream);

                var op = UnitUnderTest.ReadOp().Should().BeOfType<PingOp>().Subject;

                op.Marker.Should().Be("PING");
            }
        }

        [Fact]
        public void Should_be_able_to_parse_PongOp()
        {
            using (var stream = CreateStream(
                "PONG\r\n"))
            {
                UnitUnderTest = NatsOpStreamReader.Use(stream);

                var op = UnitUnderTest.ReadOp().Should().BeOfType<PongOp>().Subject;

                op.Marker.Should().Be("PONG");
            }
        }

        [Fact]
        public void Should_be_able_to_parse_ErrOp()
        {
            using (var stream = CreateStream(
                "-ERR 'Unknown Protocol Operation'\r\n"))
            {
                UnitUnderTest = NatsOpStreamReader.Use(stream);

                var op = UnitUnderTest.ReadOp().Should().BeOfType<ErrOp>().Subject;

                op.Marker.Should().Be("-ERR");
                op.Message.Should().Be("'Unknown Protocol Operation'");
            }
        }

        [Fact]
        public void Should_be_able_to_handle_blank_lines()
        {
            using (var stream = CreateStream(
                "+OK\r\n\r\n",
                "PING\r\n"))
            {
                UnitUnderTest = NatsOpStreamReader.Use(stream);

                var ops = new[] {UnitUnderTest.ReadOp(), UnitUnderTest.ReadOp()};
                ops.Should().HaveCount(2);
                ops[0].Should().BeOfType<OkOp>();
                ops[1].Should().BeOfType<PingOp>();
            }
        }

        private static MemoryStream CreateStream(params string[] ops)
        {
            var data = string.Join(string.Empty, ops);

            return new MemoryStream(Encoding.UTF8.GetBytes(data));
        }
    }
}
