namespace MyNatsClient
{
    public class Host
    {
        public const int DefaultPort = 4222;

        public string Address { get; }
        public int Port { get; }
        public Credentials Credentials { get; set; } = Credentials.Empty;

        private readonly string _toString;

        public Host(string address, int? port = null)
        {
            Address = address;
            Port = port ?? DefaultPort;
            _toString = $"{address}:{port}";
        }

        internal bool HasNonEmptyCredentials() => Credentials != null && Credentials != Credentials.Empty;

        public override string ToString()
        {
            return _toString;
        }
    }
}