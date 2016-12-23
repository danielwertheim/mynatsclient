using System;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace MyNatsClient.IntegrationTests
{
    public class ClientRequestTests : ClientIntegrationTests
    {
        private NatsClient _client;

        public ClientRequestTests()
        {
            _client = new NatsClient("tc1", ConnectionInfo);
            _client.Connect();
        }

        protected override void OnAfterEachTest()
        {
            _client?.Disconnect();
            _client?.Dispose();
            _client = null;
        }

        [Fact]
        public async Task Given_responder_exists_When_requesting_using_string_It_should_get_response()
        {
            var value = Guid.NewGuid().ToString("N");

            _client.SubWithHandler("getValue", msg => _client.Pub(msg.ReplyTo, msg.GetPayloadAsString()));

            var response = await _client.RequestAsync("getValue", value);

            response.GetPayloadAsString().Should().Be(value);
        }

        [Fact]
        public async Task Given_responder_exists_When_requesting_using_bytes_It_should_get_response()
        {
            var value = Guid.NewGuid().ToString("N");

            _client.SubWithHandler("getValue", msg => _client.Pub(msg.ReplyTo, msg.GetPayloadAsString()));

            var response = await _client.RequestAsync("getValue", Encoding.UTF8.GetBytes(value));

            response.GetPayloadAsString().Should().Be(value);
        }

        [Fact]
        public async Task Given_responder_exists_When_requesting_using_payload_It_should_get_response()
        {
            var value = Guid.NewGuid().ToString("N");
            var payloadBuilder = new PayloadBuilder();
            payloadBuilder.Append(Encoding.UTF8.GetBytes(value));

            _client.SubWithHandler("getValue", msg => _client.Pub(msg.ReplyTo, msg.GetPayloadAsString()));

            var response = await _client.RequestAsync("getValue", payloadBuilder.ToPayload());

            response.GetPayloadAsString().Should().Be(value);
        }

        [Fact]
        public async Task Given_responder_exists_When_requesting_after_reconnect_It_should_get_response()
        {
            var value = Guid.NewGuid().ToString("N");

            _client.SubWithHandler("getValue", msg => _client.Pub(msg.ReplyTo, msg.GetPayloadAsString()));

            _client.Disconnect();
            _client.Connect();

            var response = await _client.RequestAsync("getValue", value);

            response.GetPayloadAsString().Should().Be(value);
        }

        [Fact]
        public async Task Given_no_responder_exists_When_requesting_It_should_get_timed_out_exception()
        {
            Exception ex = null;

            try
            {
                await _client.RequestAsync("getValue", "foo value");
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
                await _client.RequestAsync("getValue", "foo value", 100);
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