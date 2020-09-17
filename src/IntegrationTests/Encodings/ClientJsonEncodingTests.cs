using System;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient;
using MyNatsClient.Encodings.Json;
using MyNatsClient.Rx;
using Xunit;

namespace IntegrationTests.Encodings
{
    public class ClientJsonEncodingTests : Tests<DefaultContext>, IDisposable
    {
        private NatsClient _client;
        private Sync _sync;

        public ClientJsonEncodingTests(DefaultContext context)
            : base(context)
        { }

        public void Dispose()
        {
            _sync?.Dispose();
            _sync = null;

            _client?.Disconnect();
            _client?.Dispose();
            _client = null;
        }

        [Fact]
        public async Task Should_be_able_to_publish_and_consume_JSON_payloads_synchronously()
        {
            var subject = Context.GenerateSubject();
            var orgItem = EncodingTestItem.Create();
            EncodingTestItem decodedItem = null;

            _sync = Sync.MaxOne();
            _client = await Context.ConnectClientAsync();
            _client.Sub(subject, stream => stream.Subscribe(msg =>
            {
                decodedItem = msg.FromJson<EncodingTestItem>();
                _sync.Release();
            }));

            _client.PubAsJson(subject, orgItem);
            _sync.WaitForAll();

            orgItem.Should().BeEquivalentTo(decodedItem);
        }

        [Fact]
        public async Task Should_be_able_to_publish_and_consume_JSON_payloads_asynchronously()
        {
            var subject = Context.GenerateSubject();
            var orgItem = EncodingTestItem.Create();
            EncodingTestItem decodedItem = null;

            _sync = Sync.MaxOne();
            _client = await Context.ConnectClientAsync();
            await _client.SubAsync(subject, stream => stream.Subscribe(msg =>
            {
                decodedItem = msg.FromJson<EncodingTestItem>();
                _sync.Release();
            }));

            await _client.PubAsJsonAsync(subject, orgItem);
            _sync.WaitForAll();

            orgItem.Should().BeEquivalentTo(decodedItem);
        }
    }
}