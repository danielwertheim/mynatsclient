using System;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient.Encodings.Json;
using MyNatsClient.Encodings.Json.Extensions;
using Xunit;

namespace MyNatsClient.IntegrationTests.Encodings
{
    public class ClientJsonEncodingTests : ClientIntegrationTests
    {
        private NatsClient _client;

        public ClientJsonEncodingTests()
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
        public void Should_be_able_to_publish_and_consume_JSON_payloads_synchronously()
        {
            var orgItem = new TestItem { Value = Guid.NewGuid().ToString("N") };
            TestItem decodedItem = null;

            _client.SubWithHandler("ClientJsonEncodingTests", msg =>
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

            await _client.SubWithHandlerAsync("ClientJsonEncodingTests", msg =>
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