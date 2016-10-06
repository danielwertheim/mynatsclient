using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using MyNatsClient.Ops;
using Xunit;

namespace MyNatsClient.UnitTests
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
                UnitUnderTest = new NatsOpStreamReader(stream);

                var infoOp = UnitUnderTest.ReadOp().First();
                infoOp.Should().BeOfType<InfoOp>();

                var okOp = UnitUnderTest.ReadOp().First();
                okOp.Should().BeOfType<OkOp>();

                var msgOp = UnitUnderTest.ReadOp().First();
                msgOp.Should().BeOfType<MsgOp>();

                var msgWithGroupOp = UnitUnderTest.ReadOp().First();
                msgWithGroupOp.Should().BeOfType<MsgOp>();

                var pingOp = UnitUnderTest.ReadOp().First();
                pingOp.Should().BeOfType<PingOp>();

                var pongOp = UnitUnderTest.ReadOp().First();
                pongOp.Should().BeOfType<PongOp>();

                var errOp = UnitUnderTest.ReadOp().First();
                errOp.Should().BeOfType<ErrOp>();

                UnitUnderTest.ReadOp().FirstOrDefault().Should().BeNull();
            }
        }

        [Fact]
        public void Should_be_able_to_parse_InfoOp()
        {
            using (var stream = CreateStream(
                "INFO {\"server_id\":\"H8RgvFtiq2zlQTA5dB0deh\"}\r\n"))
            {
                UnitUnderTest = new NatsOpStreamReader(stream);

                var op = UnitUnderTest.ReadOp().OfType<InfoOp>().First();

                op.Code.Should().Be("INFO");
                op.Message.Should().Be("{\"server_id\":\"H8RgvFtiq2zlQTA5dB0deh\"}");
                op.GetAsString().Should().Be("INFO {\"server_id\":\"H8RgvFtiq2zlQTA5dB0deh\"}");
            }
        }

        [Fact]
        public void Should_be_able_to_parse_OkOp()
        {
            using (var stream = CreateStream(
                "+OK\r\n"))
            {
                UnitUnderTest = new NatsOpStreamReader(stream);

                var op = UnitUnderTest.ReadOp().OfType<OkOp>().First();

                op.Code.Should().Be("+OK");
                op.GetAsString().Should().Be("+OK");
            }
        }

        [Fact]
        public void Should_be_able_to_parse_MsgOp_with_new_line_in_them()
        {
            using (var stream = CreateStream(
                "MSG foo siddw1 6\r\nte\r\nst\r\n"))
            {
                UnitUnderTest = new NatsOpStreamReader(stream);

                var op = UnitUnderTest.ReadOp().OfType<MsgOp>().First();

                op.Code.Should().Be("MSG");
                op.Subject.Should().Be("foo");
                op.SubscriptionId.Should().Be("siddw1");
                op.ReplyTo.Should().BeNull();
                op.Payload.Should().HaveCount(6);
                op.GetAsString().Should().Be("MSG foo siddw1 6\r\nte\r\nst");
                op.GetPayloadAsString().Should().Be("te\r\nst");
            }
        }

        [Fact]
        public void Should_be_able_to_parse_MsgOp_with_tab_in_them()
        {
            using (var stream = CreateStream(
                "MSG foo siddw1 5\r\nte\tst\r\n"))
            {
                UnitUnderTest = new NatsOpStreamReader(stream);

                var op = UnitUnderTest.ReadOp().OfType<MsgOp>().First();

                op.Code.Should().Be("MSG");
                op.Subject.Should().Be("foo");
                op.SubscriptionId.Should().Be("siddw1");
                op.ReplyTo.Should().BeNull();
                op.Payload.Should().HaveCount(5);
                op.GetAsString().Should().Be("MSG foo siddw1 5\r\nte\tst");
                op.GetPayloadAsString().Should().Be("te\tst");
            }
        }

        [Fact]
        public void Should_be_able_to_parse_tab_delimitted_MsgOp()
        {
            using (var stream = CreateStream(
                "MSG\tfoo\tsiddw1\t4\r\ntest\r\n"))
            {
                UnitUnderTest = new NatsOpStreamReader(stream);

                var op = UnitUnderTest.ReadOp().OfType<MsgOp>().First();

                op.Code.Should().Be("MSG");
                op.Subject.Should().Be("foo");
                op.SubscriptionId.Should().Be("siddw1");
                op.ReplyTo.Should().BeNull();
                op.Payload.Should().HaveCount(4);
                op.GetAsString().Should().Be("MSG foo siddw1 4\r\ntest");
                op.GetPayloadAsString().Should().Be("test");
            }
        }

        [Fact]
        public void Should_be_able_to_parse_PingOp()
        {
            using (var stream = CreateStream(
                "PING\r\n"))
            {
                UnitUnderTest = new NatsOpStreamReader(stream);

                var op = UnitUnderTest.ReadOp().OfType<PingOp>().First();

                op.Code.Should().Be("PING");
                op.GetAsString().Should().Be("PING");
            }
        }

        [Fact]
        public void Should_be_able_to_parse_PongOp()
        {
            using (var stream = CreateStream(
                "PONG\r\n"))
            {
                UnitUnderTest = new NatsOpStreamReader(stream);

                var op = UnitUnderTest.ReadOp().OfType<PongOp>().First();

                op.Code.Should().Be("PONG");
                op.GetAsString().Should().Be("PONG");
            }
        }

        [Fact]
        public void Should_be_able_to_parse_ErrOp()
        {
            using (var stream = CreateStream(
                "-ERR 'Unknown Protocol Operation'\r\n"))
            {
                UnitUnderTest = new NatsOpStreamReader(stream);

                var op = UnitUnderTest.ReadOp().OfType<ErrOp>().First();

                op.Code.Should().Be("-ERR");
                op.GetAsString().Should().Be("-ERR 'Unknown Protocol Operation'");
            }
        }

        [Fact]
        public void Should_be_able_to_handle_blank_ops()
        {
            using (var stream = CreateStream(
                "+OK\r\n\r\n",
                "PING\r\n"))
            {
                UnitUnderTest = new NatsOpStreamReader(stream);

                var ops = UnitUnderTest.ReadOp().ToArray();
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