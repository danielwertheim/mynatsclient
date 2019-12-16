using System;

namespace MyNatsClient
{
    public class Host
    {
        public const int DefaultPort = 4222;

        public string Address { get; }
        public int Port { get; }
        public Credentials Credentials { get; set; } = Credentials.Empty;

        public Host(string address, int? port = null)
        {
            Address = address ?? throw new ArgumentException("Address must be specified.", nameof(address));
            Port = port ?? DefaultPort;
        }

        internal bool HasNonEmptyCredentials()
            => Credentials != null && Credentials != Credentials.Empty;

        public override string ToString() => $"{Address}:{Port}";
    }
}