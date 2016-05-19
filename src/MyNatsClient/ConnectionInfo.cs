using System.Linq;

namespace NatsFun
{
    public class ConnectionInfo
    {
        public string ClientId { get; }
        public Host[] Hosts { get; }
        public bool Verbose { get; set; }
        public bool AutoRespondToPing { get; set; } = true;

        public ConnectionInfo(string clientId, Host[] hosts)
        {
            ClientId = clientId;
            Hosts = hosts;
        }

        public ConnectionInfo Clone()
        {
            var hosts = Hosts
                .Select(i => new Host(i.Address, i.Port))
                .ToArray();

            return new ConnectionInfo(ClientId, hosts)
            {
                AutoRespondToPing = AutoRespondToPing,
                Verbose = Verbose
            };
        }
    }
}