using System.Linq;

namespace MyNatsClient
{
    public class ConnectionInfo
    {
        public Host[] Hosts { get; }
        public bool AutoRespondToPing { get; set; } = true;
        public Credentials Credentials { get; set; } = Credentials.Empty;
        public bool Verbose { get; set; }
        public PubFlushMode PubFlushMode { get; set; } = PubFlushMode.Auto;
        public SocketOptions SocketOptions { get; set; } = new SocketOptions();

        public ConnectionInfo(Host host) : this(new[] { host }) { }

        public ConnectionInfo(Host[] hosts)
        {
            Hosts = hosts;
        }

        public ConnectionInfo Clone()
        {
            var hosts = Hosts
                .Select(i => new Host(i.Address, i.Port)
                {
                    Credentials = i.Credentials != null
                        ? new Credentials(i.Credentials.User, i.Credentials.Pass)
                        : Credentials.Empty
                })
                .ToArray();

            return new ConnectionInfo(hosts)
            {
                AutoRespondToPing = AutoRespondToPing,
                Credentials = new Credentials(Credentials.User, Credentials.Pass),
                Verbose = Verbose,
                PubFlushMode = PubFlushMode,
                SocketOptions = new SocketOptions
                {
                    ReceiveBufferSize = SocketOptions.ReceiveBufferSize,
                    SendBufferSize = SocketOptions.SendBufferSize,
                    ReceiveTimeoutMs = SocketOptions.ReceiveTimeoutMs,
                    SendTimeoutMs = SocketOptions.SendTimeoutMs
                }
            };
        }
    }
}