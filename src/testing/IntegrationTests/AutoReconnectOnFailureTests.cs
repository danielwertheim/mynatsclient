using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyNatsClient;
using MyNatsClient.Events;
using MyNatsClient.Rx;
using Xunit;

namespace IntegrationTests
{
    public class AutoReconnectOnFailureTests : Tests<DefaultContext>, IDisposable
    {
        private NatsClient _client;
        private Sync _sync;

        public AutoReconnectOnFailureTests(DefaultContext context)
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
        public async Task Client_Should_not_reconnect_When_user_initiated_disconnect()
        {
            var wasDisconnectedDueToFailure = false;
            var wasDisconnected = false;
            var wasReconnected = false;

            _sync = Sync.MaxTwo();

            var cnInfo = Context.GetConnectionInfo();
            cnInfo.AutoReconnectOnFailure = true;
            _client = new NatsClient(cnInfo);
            await _client.ConnectAsync();

            _client.Events
                .OfType<ClientDisconnected>()
                .Subscribe(ev =>
                {
                    wasDisconnectedDueToFailure = ev.Reason == DisconnectReason.DueToFailure;
                    wasDisconnected = true;
                    _sync.Release();
                });

            _client.Events
                .OfType<ClientConnected>()
                .Subscribe(ev =>
                {
                    wasReconnected = true;
                    _sync.Release();
                });

            _client.Disconnect();

            //Wait for the Disconnected release and the potential Connected release
            _sync.WaitForAny();

            wasDisconnectedDueToFailure.Should().BeFalse();
            wasDisconnected.Should().BeTrue();
            wasReconnected.Should().BeFalse();
            _client.IsConnected.Should().BeFalse();
        }
    }
}
