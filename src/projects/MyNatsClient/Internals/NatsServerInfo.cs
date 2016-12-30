using System;
using System.Collections.Generic;
using System.Text;

namespace MyNatsClient.Internals
{
    public class NatsServerInfo
    {
        public string ServerId { get; private set; }
        public string Version { get; private set; }
        public string Go { get; private set; }
        public string Host { get; private set; }
        public int Port { get; private set; }
        public bool AuthRequired { get; private set; }
        public bool SslRequired { get; private set; }
        public bool TlsRequired { get; private set; }
        public bool TlsVerify { get; private set; }
        public long MaxPayload { get; private set; }
        public List<string> ConnectUrls { get; } = new List<string>();

        private NatsServerInfo() { }

        public static NatsServerInfo Parse(string data)
        {
            var parts = SplitToKeyValues(data);

            var result = new NatsServerInfo();
            string tmp;

            if (parts.TryGetValue("server_id", out tmp))
                result.ServerId = tmp;

            if (parts.TryGetValue("version", out tmp))
                result.Version = tmp;

            if (parts.TryGetValue("go", out tmp))
                result.Go = tmp;

            if (parts.TryGetValue("host", out tmp))
                result.Host = tmp;

            if (parts.TryGetValue("port", out tmp))
                result.Port = int.Parse(tmp);

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

            if (parts.TryGetValue("connect_urls", out tmp) && !string.IsNullOrWhiteSpace(tmp))
            {
                var urls = tmp.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                result.ConnectUrls.AddRange(urls);
            }

            return result;
        }

        private static Dictionary<string, string> SplitToKeyValues(string data)
        {
            var kv = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(data))
                return kv;

            string key = null;
            var current = new StringBuilder();

            for (var charIndex = 0; charIndex < data.Length; charIndex++)
            {
                var c = data[charIndex];
                if (c == '{')
                    continue;

                if (c == '"')
                    continue;

                if (c == '}')
                    continue;

                if (c == '[')
                {
                    //Only support one level arrays for now...
                    var closingArrayAt = data.IndexOf(']', charIndex + 1);
                    var value = data.Substring(charIndex + 1, closingArrayAt - (charIndex + 1));
                    kv.Add(key, (value ?? string.Empty).Replace("\"", string.Empty));
                    key = null;
                    current = current.Clear();
                    charIndex = closingArrayAt;
                    continue;
                }

                if (c == ':')
                {
                    key = current.ToString();
                    current = current.Clear();
                    continue;
                }

                if (c == ',')
                {
                    if (key != null)
                    {
                        kv.Add(key, current.ToString());
                        key = null;
                    }

                    current = current.Clear();
                    continue;
                }

                current.Append(c);
            }

            if (!string.IsNullOrWhiteSpace(key))
                kv.Add(key, current.ToString());

            return kv;
        }
    }
}