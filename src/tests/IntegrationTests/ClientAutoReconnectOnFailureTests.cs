using System;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient;
using MyNatsClient.Events;
using MyNatsClient.Extensions;
using Xunit;

namespace IntegrationTests
{
    public class ClientAutoReconnectOnFailureTests : ClientIntegrationTests
    {
        private NatsClient _client;
        private readonly ConnectionInfo _cnInfoWithAutoReconnect;
        private readonly ConnectionInfo _cnInfoWithNoAutoReconnect;

        public ClientAutoReconnectOnFailureTests()
        {
            _cnInfoWithAutoReconnect = ConnectionInfo.Clone();
            _cnInfoWithAutoReconnect.AutoReconnectOnFailure = true;

            _cnInfoWithNoAutoReconnect = ConnectionInfo.Clone();
            _cnInfoWithNoAutoReconnect.AutoReconnectOnFailure = false;
        }

        protected override void OnAfterEachTest()
        {
            _client?.Disconnect();
            _client?.Dispose();
            _client = null;
        }

        [Fact]
        public async Task Client_Should_reconnect_after_failure_When_configured_to_do_so()
        {
            const string subject = "test";
            var wasDisconnectedDueToFailure = false;
            var wasReconnected = false;

            _client = new NatsClient(_cnInfoWithAutoReconnect);
            _client.Connect();

            await _client.SubAsync(subject);

            _client.Events.OfType<ClientDisconnected>()
                .Where(ev => ev.Reason == DisconnectReason.DueToFailure)
                .Subscribe(ev =>
                {
                    wasDisconnectedDueToFailure = true;
                    ReleaseOne();
                });

            _client.Events.OfType<ClientConnected>()
                .Subscribe(ev =>
                {
                    wasReconnected = true;
                    ReleaseOne();
                });

            _client.MsgOpStream.Subscribe(msg => throw new Exception("FAIL"));

            await _client.PubAsync(subject, "This message will fail");

            //Wait for the Disconnected release and the Connected release
            WaitOne();
            WaitOne();

            wasDisconnectedDueToFailure.Should().BeTrue();
            wasReconnected.Should().BeTrue();
            _client.IsConnected.Should().BeTrue();
        }

        [Fact]
        public async Task Client_Should_not_reconnect_after_failure_When_not_configured_to_do_so()
        {
            const string subject = "test";
            var wasDisconnectedDueToFailure = false;
            var wasReconnected = false;

            _client = new NatsClient(_cnInfoWithNoAutoReconnect);
            _client.Connect();

            await _client.SubAsync(subject);

            _client.Events.OfType<ClientDisconnected>()
                .Where(ev => ev.Reason == DisconnectReason.DueToFailure)
                .Subscribe(ev =>
                {
                    wasDisconnectedDueToFailure = true;
                    ReleaseOne();
                });

            _client.Events.OfType<ClientConnected>()
                .Subscribe(ev =>
                {
                    wasReconnected = true;
                    ReleaseOne();
                });

            _client.MsgOpStream.Subscribe(msg => throw new Exception("Fail"));

            await _client.PubAsync(subject, "This message will fail");

            //Wait for the Disconnected release and the potential Connected release
            WaitOne();
            WaitOne();

            wasDisconnectedDueToFailure.Should().BeTrue();
            wasReconnected.Should().BeFalse();
            _client.IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task Client_Should_not_reconnect_When_user_initiated_disconnect()
        {
            const string subject = "test";
            var wasDisconnectedDueToFailure = false;
            var wasDisconnected = false;
            var wasReconnected = false;

            _client = new NatsClient(_cnInfoWithAutoReconnect);
            _client.Connect();

            await _client.SubAsync(subject);

            _client.Events.OfType<ClientDisconnected>()
                .Subscribe(ev =>
                {
                    wasDisconnectedDueToFailure = ev.Reason == DisconnectReason.DueToFailure;
                    wasDisconnected = true;
                    ReleaseOne();
                });

            _client.Events.OfType<ClientConnected>()
                .Subscribe(ev =>
                {
                    wasReconnected = true;
                    ReleaseOne();
                });

            _client.Disconnect();

            //Wait for the Disconnected release and the potentiall Connected release
            WaitOne();
            WaitOne();

            wasDisconnectedDueToFailure.Should().BeFalse();
            wasDisconnected.Should().BeTrue();
            wasReconnected.Should().BeFalse();
            _client.IsConnected.Should().BeFalse();
        }
    }
}