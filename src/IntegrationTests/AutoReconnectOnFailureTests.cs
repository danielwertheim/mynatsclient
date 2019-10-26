using System;
using System.Threading.Tasks;
using FluentAssertions;
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

        [Fact(Skip = "Manual")]
        public async Task Client_Should_reconnect_after_failure_When_configured_to_do_so()
        {
            var subject = Context.GenerateSubject();
            var wasDisconnectedDueToFailure = false;
            var wasReconnected = false;

            var ex = new Exception("This will destroy things.");
            var throwingLogger = new Mock<ILogger>();
            throwingLogger.Setup(f => f.Error(It.IsAny<string>(), It.IsAny<Exception>())).Throws(ex);
            LoggerManager.Resolve = type => throwingLogger.Object;

            _sync = Sync.MaxTwo();

            var cnInfo = Context.GetConnectionInfo();
            cnInfo.AutoReconnectOnFailure = true;
            _client = new NatsClient(cnInfo);
            await _client.ConnectAsync();

            await _client.SubAsync(subject);

            _client.Events
                .OfType<ClientDisconnected>()
                .Where(ev => ev.Reason == DisconnectReason.DueToFailure)
                .Subscribe(ev =>
                {
                    wasDisconnectedDueToFailure = true;
                    _sync.Release();
                });

            _client.Events.OfType<ClientConnected>()
                .Subscribe(ev =>
                {
                    wasReconnected = true;
                    _sync.Release();
                });

            _client.MsgOpStream.Subscribe(msg => throw new Exception("Fail"));

            await _client.PubAsync(subject, "This message will fail");

            //Wait for the Disconnected release and the Connected release
            _sync.WaitForAll();

            wasDisconnectedDueToFailure.Should().BeTrue();
            wasReconnected.Should().BeTrue();
            _client.IsConnected.Should().BeTrue();
        }

        [Fact(Skip = "Manual")]
        public async Task Client_Should_not_reconnect_after_failure_When_not_configured_to_do_so()
        {
            var subject = Context.GenerateSubject();
            var wasDisconnectedDueToFailure = false;
            var wasReconnected = false;

            var ex = new Exception("This will destroy things.");
            var throwingLogger = new Mock<ILogger>();
            throwingLogger.Setup(f => f.Error(It.IsAny<string>(), It.IsAny<Exception>())).Throws(ex);
            LoggerManager.Resolve = type => throwingLogger.Object;

            _sync = Sync.MaxTwo();

            var cnInfo = Context.GetConnectionInfo();
            cnInfo.AutoReconnectOnFailure = false;
            _client = new NatsClient(cnInfo);
            await _client.ConnectAsync();

            await _client.SubAsync(subject);

            _client.Events
                .OfType<ClientDisconnected>()
                .Where(ev => ev.Reason == DisconnectReason.DueToFailure)
                .Subscribe(ev =>
                {
                    wasDisconnectedDueToFailure = true;
                    _sync.Release();
                });

            _client.Events
                .OfType<ClientConnected>()
                .Subscribe(ev =>
                {
                    wasReconnected = true;
                    _sync.Release();
                });

            _client.MsgOpStream.Subscribe(msg => throw new Exception("Fail"));

            await _client.PubAsync(subject, "This message will fail");

            //Wait for the Disconnected release and the potential Connected release
            _sync.WaitForAny();

            wasDisconnectedDueToFailure.Should().BeTrue();
            wasReconnected.Should().BeFalse();
            _client.IsConnected.Should().BeFalse();
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