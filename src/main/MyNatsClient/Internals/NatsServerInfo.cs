using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace MyNatsClient.Internals
{
    public class NatsServerInfo : INatsServerInfo
    {
        public string ServerId { get; private set; }
        public string Version { get; private set; }
        public string Go { get; private set; }
        public string Host { get; private set; }
        public int Port { get; private set; }
        public bool AuthRequired { get; private set; }
        public bool TlsRequired { get; private set; }
        public bool TlsVerify { get; private set; }
        public int MaxPayload { get; private set; }
        public List<string> ConnectUrls { get; } = new List<string>();
        public string Ip { get; set; }

        private NatsServerInfo() { }

        public static NatsServerInfo Parse(ReadOnlyMemory<char> data)
        {
            var result = new NatsServerInfo();

            using var doc = JsonDocument.Parse(data, new JsonDocumentOptions { AllowTrailingCommas = true });

            var root = doc.RootElement;

            if (root.TryGetProperty("server_id", out var el))
                result.ServerId = el.GetString();

            if (root.TryGetProperty("version", out el))
                result.Version = el.GetString();

            if (root.TryGetProperty("go", out el))
                result.Go = el.GetString();

            if (root.TryGetProperty("host", out el))
                result.Host = el.GetString();

            if (root.TryGetProperty("port", out el))
                result.Port = el.GetInt32();

            if (root.TryGetProperty("auth_required", out el))
                result.AuthRequired = el.GetBoolean();

            if (root.TryGetProperty("tls_required", out el))
                result.TlsRequired = el.GetBoolean();

            if (root.TryGetProperty("tls_verify", out el))
                result.TlsVerify = el.GetBoolean();

            if (root.TryGetProperty("max_payload", out el))
                result.MaxPayload = el.GetInt32();

            if (root.TryGetProperty("connect_urls", out el))
                result.ConnectUrls.AddRange(el.EnumerateArray().Select(i => i.GetString()));

            if (root.TryGetProperty("ip", out el))
                result.Ip = el.GetString();

            return result;
        }
    }
}