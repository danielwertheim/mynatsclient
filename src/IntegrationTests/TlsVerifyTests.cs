using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FluentAssertions;
using IntegrationTests.Extension;
using MyNatsClient;
using MyNatsClient.Rx;
using Xunit;

namespace IntegrationTests
{
    public class TlsVerifyTests : Tests<TlsVerifyContext>, IDisposable
    {
        private NatsClient _requester;
        private NatsClient _responder;

        public TlsVerifyTests(TlsVerifyContext context)
            : base(context)
        {
        }

        public void Dispose()
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

            var connectionInfo = Context
                .GetConnectionInfo()
                .AllowAllServerCertificates();

            var clientCert = new X509Certificate2(Path.Combine("Resources", "client.pfx"));
            connectionInfo.ClientCertificates.Add(clientCert);

            _responder = await Context.ConnectClientAsync(connectionInfo);
            _requester = await Context.ConnectClientAsync(connectionInfo);

            _responder.Sub("getValue", stream => stream.Subscribe(msg => _responder.Pub(msg.ReplyTo, msg.GetPayloadAsString())));

            var response = await _requester.RequestAsync("getValue", value);

            response.GetPayloadAsString().Should().Be(value);
        }
    }
}
