using System.Linq;
using EnsureThat;

namespace MyNatsClient
{
    public class ConnectionInfo
    {
        /// <summary>
        /// Gets the hosts that a client can randomly connect to.
        /// </summary>
        public Host[] Hosts { get; }

        /// <summary>
        /// Gets or sets value indicating if client should
        /// respond to server pings automatically.
        /// Default is true.
        /// </summary>
        public bool AutoRespondToPing { get; set; } = true;

        /// <summary>
        /// Gets or sets value indicating if client should
        /// try and auto reconnect on failure.
        /// Default is false.
        /// </summary>
        public bool AutoReconnectOnFailure { get; set; } = false;

        /// <summary>
        /// Gets or sets the credentials used when connecting against the hosts.
        /// </summary>
        /// <remarks>You can specify host specific credentials on each host.</remarks>
        public Credentials Credentials { get; set; } = Credentials.Empty;

        /// <summary>
        /// Gets or sets value if verbose output should be used.
        /// Default is false.
        /// </summary>
        public bool Verbose { get; set; } = false;

        /// <summary>
        /// Gets or sets value determining how the clients flush behavior
        /// should be when sending messages. E.g. when Pub or PubAsync is called.
        /// Default is Auto (will Flush after each Pub or PubAsync).
        /// </summary>
        public PubFlushMode PubFlushMode { get; set; } = PubFlushMode.Auto;

        public SocketOptions SocketOptions { get; set; } = new SocketOptions();

        public ConnectionInfo(Host host) : this(new[] { host }) { }

        public ConnectionInfo(Host[] hosts)
        {
            EnsureArg.HasItems(hosts, nameof(hosts));

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
                AutoReconnectOnFailure = AutoReconnectOnFailure,
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