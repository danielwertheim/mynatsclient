using System.Linq;

namespace MyNatsClient
{
    public class ConnectionInfo
    {
        public Host[] Hosts { get; }
        public bool AutoRespondToPing { get; set; } = true;
        public Credentials Credentials { get; set; } = Credentials.Empty;
        public bool Verbose { get; set; }

        public ConnectionInfo(Host host) : this(new[] { host }) { }

        public ConnectionInfo(Host[] hosts)
        {
            Hosts = hosts;
        }

        public ConnectionInfo Clone()
        {
            var hosts = Hosts
                .Select(i => new Host(i.Address, i.Port))
                .ToArray();

            return new ConnectionInfo(hosts)
            {
                AutoRespondToPing = AutoRespondToPing,
                Credentials = new Credentials(Credentials.User, Credentials.Pass),
                Verbose = Verbose
            };
        }
    }
}