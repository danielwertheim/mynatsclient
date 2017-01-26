using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient.Ops;
using Xunit;

namespace MyNatsClient.IntegrationTests
{
    public class ClientRequestTests : ClientIntegrationTests
    {
        private NatsClient _requester;
        private NatsClient _responder;

        public ClientRequestTests()
        {
            _requester = new NatsClient("requester", ConnectionInfo);
            _requester.Connect();

            _responder = new NatsClient("responder", ConnectionInfo);
            _responder.Connect();
        }

        protected override void OnAfterEachTest()
        {
            _requester?.Disconnect();
            _requester?.Dispose();
            _requester = null;

            _responder?.Disconnect();
            _responder?.Dispose();
            _responder = null;
        }

        [Fact]
        public async Task Given_responder_exists_When_requesting_using_string_It_should_get_response()
        {
            var value = Guid.NewGuid().ToString("N");

            _responder.SubWithHandler("getValue", msg => _responder.Pub(msg.ReplyTo, msg.GetPayloadAsString()));

            var response = await _requester.RequestAsync("getValue", value);

            response.GetPayloadAsString().Should().Be(value);
        }

        [Fact]
        public async Task Given_responder_exists_When_requesting_using_bytes_It_should_get_response()
        {
            var value = Guid.NewGuid().ToString("N");

            _responder.SubWithHandler("getValue", msg => _responder.Pub(msg.ReplyTo, msg.GetPayloadAsString()));

            var response = await _requester.RequestAsync("getValue", Encoding.UTF8.GetBytes(value));

            response.GetPayloadAsString().Should().Be(value);
        }

        [Fact]
        public async Task Given_responder_exists_When_requesting_using_payload_It_should_get_response()
        {
            var value = Guid.NewGuid().ToString("N");
            var payloadBuilder = new PayloadBuilder();
            payloadBuilder.Append(Encoding.UTF8.GetBytes(value));

            _responder.SubWithHandler("getValue", msg => _responder.Pub(msg.ReplyTo, msg.GetPayloadAsString()));

            var response = await _requester.RequestAsync("getValue", payloadBuilder.ToPayload());

            response.GetPayloadAsString().Should().Be(value);
        }

        [Fact]
        public async Task Given_multiple_responders_exists_When_requesting_It_should_return_one_response()
        {
            var value = Guid.NewGuid().ToString("N");
            var responderReplyingCount = 0;
            var responderReplyCount = 0;

            _requester.MsgOpStream.Subscribe(msgOp => Interlocked.Increment(ref responderReplyCount));

            _responder.SubWithHandler("getValue", msg =>
            {
                Interlocked.Increment(ref responderReplyingCount);
                _responder.Pub(msg.ReplyTo, msg.GetPayloadAsString());
            });

            MsgOp response;
            using (var responder2 = new NatsClient("Responder2", ConnectionInfo))
            {
                responder2.Connect();
                responder2.SubWithHandler("getValue", msg =>
                {
                    Interlocked.Increment(ref responderReplyingCount);
                    responder2.Pub(msg.ReplyTo, msg.GetPayloadAsString());
                });

                response = await _requester.RequestAsync("getValue", value);
            }

            response.GetPayloadAsString().Should().Be(value);
            responderReplyCount.Should().Be(1);
            responderReplyingCount.Should().Be(2);
        }

        [Fact]
        public async Task Given_no_responder_exists_When_requesting_It_should_get_timed_out_exception()
        {
            Exception ex = null;

            try
            {
                await _requester.RequestAsync("getValue", "foo value");
            }
            catch (NatsException e)
            {
                ex = e;
            }

            ex.Should().NotBeNull();
            ex.Should().BeOfType<NatsRequestTimedOutException>();
        }

        [Fact]
        public async Task Given_no_responder_exists_When_requesting_with_explicit_time_out_It_should_get_timed_out_exception()
        {
            Exception ex = null;

            try
            {
                await _requester.RequestAsync("getValue", "foo value", 100);
            }
            catch (NatsException e)
            {
                ex = e;
            }

            ex.Should().NotBeNull();
            ex.Should().BeOfType<NatsRequestTimedOutException>();
        }
    }
}