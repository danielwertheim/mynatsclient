using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient;
using MyNatsClient.Encodings.Json;
using MyNatsClient.Extensions;
using Xunit;

namespace IntegrationTests.Encodings
{
    public class ClientProtobufEncodingTests : ClientIntegrationTests
    {
        private const string Subject = nameof(ClientProtobufEncodingTests);

        private NatsClient _client;

        public ClientProtobufEncodingTests()
        {
            _client = new NatsClient(ConnectionInfo);
            _client.Connect();
        }

        protected override void OnAfterEachTest()
        {
            _client?.Disconnect();
            _client?.Dispose();
            _client = null;
        }

        [Fact]
        public void Should_be_able_to_publish_and_consume_JSON_payloads_synchronously()
        {
            var orgItem = EncodingTestItem.Create();
            EncodingTestItem decodedItem = null;

            _client.Sub("ClientProtobufEncodingTests", stream => stream.Subscribe(msg =>
            {
                decodedItem = msg.FromJson<EncodingTestItem>();
                ReleaseOne();
            }));

            _client.PubAsJson("ClientProtobufEncodingTests", orgItem);
            WaitOne();

            orgItem.Should().BeEquivalentTo(decodedItem);
        }

        [Fact]
        public async Task Should_be_able_to_publish_and_consume_JSON_payloads_asynchronously()
        {
            var orgItem = EncodingTestItem.Create();
            EncodingTestItem decodedItem = null;

            await _client.SubAsync("ClientProtobufEncodingTests", stream => stream.Subscribe(msg =>
            {
                decodedItem = msg.FromJson<EncodingTestItem>();
                ReleaseOne();
            }));

            await _client.PubAsJsonAsync("ClientProtobufEncodingTests", orgItem);
            WaitOne();

            orgItem.Should().BeEquivalentTo(decodedItem);
        }
    }
}