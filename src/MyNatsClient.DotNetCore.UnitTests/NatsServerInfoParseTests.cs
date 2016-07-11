﻿using FluentAssertions;
using MyNatsClient.Internals;
using NUnit.Framework;

namespace MyNatsClient.UnitTests
{
    public class NatsServerInfoParseTests : UnitTests
    {
        [Test]
        public void Should_be_able_to_parse_server_info_When_snake_cased_json_is_returned()
        {
            var parsed = NatsServerInfo.Parse("{\"server_id\":\"Vwp6WDR1NIEuFr0CQ9PtMa\",\"version\":\"0.8.0\",\"go\":\"go1.6.2\",\"host\":\"0.0.0.0\",\"port\":4222,\"auth_required\":false,\"ssl_required\":false,\"tls_required\":false,\"tls_verify\":false,\"max_payload\":1048576}");

            parsed.ServerId.Should().Be("Vwp6WDR1NIEuFr0CQ9PtMa");
            parsed.Version.Should().Be("0.8.0");
            parsed.Go.Should().Be("go1.6.2");
            parsed.AuthRequired.Should().BeFalse();
            parsed.SslRequired.Should().BeFalse();
            parsed.TlsRequired.Should().BeFalse();
            parsed.TlsVerify.Should().BeFalse();
            parsed.MaxPayload.Should().Be(1048576);

            parsed = NatsServerInfo.Parse("{\"auth_required\":true,\"ssl_required\":true,\"tls_required\":true,\"tls_verify\":true}");
            parsed.AuthRequired.Should().BeTrue();
            parsed.SslRequired.Should().BeTrue();
            parsed.TlsRequired.Should().BeTrue();
            parsed.TlsVerify.Should().BeTrue();
        }
    }
}