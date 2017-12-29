using System;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient;
using MyNatsClient.Encodings.Json;
using Xunit;

namespace IntegrationTests.Encodings
{
    public class ClientJsonEncodingTests : ClientIntegrationTests
    {
        private NatsClient _client;

        public ClientJsonEncodingTests()
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
            var orgItem = new TestItem { Value = Guid.NewGuid().ToString("N") };
            TestItem decodedItem = null;

            _client.Sub("ClientJsonEncodingTests", msg =>
            {
                decodedItem = msg.FromJson<TestItem>();
                ReleaseOne();
            });

            _client.PubAsJson("ClientJsonEncodingTests", orgItem);
            WaitOne();

            orgItem.ShouldBeEquivalentTo(decodedItem);
        }

        [Fact]
        public async Task Should_be_able_to_publish_and_consume_JSON_payloads_asynchronously()
        {
            var orgItem = new TestItem { Value = Guid.NewGuid().ToString("N") };
            TestItem decodedItem = null;

            await _client.SubAsync("ClientJsonEncodingTests", msg =>
            {
                decodedItem = msg.FromJson<TestItem>();
                ReleaseOne();
            });

            await _client.PubAsJsonAsync("ClientJsonEncodingTests", orgItem);
            WaitOne();

            orgItem.ShouldBeEquivalentTo(decodedItem);
        }

        private class TestItem
        {
            public string Value { get; set; }
        }
    }
}