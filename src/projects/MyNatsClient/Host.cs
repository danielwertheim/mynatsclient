namespace MyNatsClient
{
    public class Host
    {
        public string Address { get; }
        public int Port { get; }
        public Credentials Credentials { get; set; } = Credentials.Empty;

        private readonly string _toString;

        public Host(string address, int port = 4222)
        {
            Address = address;
            Port = port;
            _toString = $"{address}:{port}";
        }

        internal bool HasNonEmptyCredentials() => Credentials != null && Credentials != Credentials.Empty;

        public override string ToString()
        {
            return _toString;
        }
    }
}