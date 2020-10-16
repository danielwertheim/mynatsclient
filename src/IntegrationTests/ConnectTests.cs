using System;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient;
using Xunit;

namespace IntegrationTests
{
    public class ConnectTests : Tests<DefaultContext>, IDisposable
    {
        private NatsClient _client;
        private Sync _sync;

        public ConnectTests(DefaultContext context)
            : base(context)
        {
        }

        public void Dispose()
        {
            _sync?.Dispose();
            _sync = null;

            _client?.Disconnect();
            _client?.Dispose();
            _client = null;
        }
        
        [Fact]
        public async Task Given_not_connected_Should_be_able_to_connect_with_specific_name()
        {
            var connectionInfo = Context.GetConnectionInfo();
            connectionInfo.Name = Guid.NewGuid().ToString("N");
            
            _client = await Context.ConnectClientAsync(connectionInfo);
            
            await Context.DelayAsync();

            _client.IsConnected.Should().BeTrue();
        }
    }
}
