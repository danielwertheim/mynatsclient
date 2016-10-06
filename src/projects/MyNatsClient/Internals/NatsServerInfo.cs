using System;
using System.Linq;

namespace MyNatsClient.Internals
{
    public class NatsServerInfo
    {
        public string ServerId { get; private set; }
        public string Version { get; private set; }
        public string Go { get; private set; }
        public bool AuthRequired { get; private set; }
        public bool SslRequired { get; private set; }
        public bool TlsRequired { get; private set; }
        public bool TlsVerify { get; private set; }
        public long MaxPayload { get; private set; }

        private NatsServerInfo() { }

        public static NatsServerInfo Parse(string data)
        {
            var parts = data
                .Trim()
                .TrimStart('{')
                .TrimEnd('}')
                .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                .Select(pair => pair.Split(new[] { "\":" }, StringSplitOptions.RemoveEmptyEntries))
                .ToDictionary(
                    i => i[0].Trim().TrimStart('"').TrimEnd('"').Trim(),
                    i => i[1].Trim().TrimStart('"').TrimEnd('"').Trim());

            var result = new NatsServerInfo();
            string tmp;

            if (parts.TryGetValue("server_id", out tmp))
                result.ServerId = tmp;

            if (parts.TryGetValue("version", out tmp))
                result.Version = tmp;

            if (parts.TryGetValue("go", out tmp))
                result.Go = tmp;

            if (parts.TryGetValue("auth_required", out tmp))
                result.AuthRequired = tmp == "true";

            if (parts.TryGetValue("ssl_required", out tmp))
                result.SslRequired = tmp == "true";

            if (parts.TryGetValue("tls_required", out tmp))
                result.TlsRequired = tmp == "true";

            if (parts.TryGetValue("tls_verify", out tmp))
                result.TlsVerify = tmp == "true";

            if (parts.TryGetValue("max_payload", out tmp))
                result.MaxPayload = int.Parse(tmp);

            return result;
        }
    }
}