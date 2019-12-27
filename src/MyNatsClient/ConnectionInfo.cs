using System;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace MyNatsClient
{
    public class ConnectionInfo
    {
        /// <summary>
        /// Gets the hosts that a client can randomly connect to.
        /// </summary>
        public Host[] Hosts { get; }

        /// <summary>
        /// When enabled (default), one single global Inbox-subsription is
        /// initiated against the NATS-Server upon first request. After that,
        /// all responses are reported back to that inbox.
        /// If disabled, one subsription is initated and disposed per request.
        /// </summary>
        public bool UseInboxRequests { get; set; } = true;

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
        public bool AutoReconnectOnFailure { get; set; } = true;

        /// <summary>
        /// Gets or sets the credentials used when connecting against the hosts.
        /// </summary>
        /// <remarks>You can specify host specific credentials on each host.</remarks>
        public Credentials Credentials { get; set; } = Credentials.Empty;

        /// <summary>
        /// Gets or sets value if verbose output should be used.
        /// Default is false.
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Gets or sets value determining how the clients flush behavior
        /// should be when sending messages. E.g. when Pub or PubAsync is called.
        /// Default is Auto (will Flush after each Pub or PubAsync).
        /// </summary>
        public PubFlushMode PubFlushMode { get; set; } = PubFlushMode.Auto;

        /// <summary>
        /// Gets or sets the default value to use for request timeout.
        /// </summary>
        public int RequestTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// Gets or sets certificate collection used when authenticating the client against the server
        /// when the server is configured to use TLS and to verify the clients.
        /// If the server is onyl configured to use TLS but not configured to verfify the
        /// clients, no client certificates need to be provided.
        /// </summary>
        public X509Certificate2Collection ClientCertificates { get; set; } = new X509Certificate2Collection();

        /// <summary>
        /// Gets or sets custom handler to use for verifying the server certificate.
        /// This is used if the server is configured to use TLS.
        /// </summary>
        public Func<X509Certificate, X509Chain, SslPolicyErrors, bool> ServerCertificateValidation { get; set; }

        /// <summary>
        /// Gets or sets <see cref="SocketOptions"/> to use when creating the clients
        /// underlying socket via <see cref="ISocketFactory"/>.
        /// </summary>
        public SocketOptions SocketOptions { get; set; } = new SocketOptions();

        public ConnectionInfo(string host, int? port = null)
            : this(new Host(host, port)) { }

        public ConnectionInfo(Host host)
            : this(new[] { host }) { }

        public ConnectionInfo(Host[] hosts)
        {
            if (hosts?.Any() == false)
                throw new ArgumentException("At least one host need to be specified.", nameof(hosts));

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
                UseInboxRequests = UseInboxRequests,
                AutoRespondToPing = AutoRespondToPing,
                AutoReconnectOnFailure = AutoReconnectOnFailure,
                Credentials = new Credentials(Credentials.User, Credentials.Pass),
                Verbose = Verbose,
                RequestTimeoutMs = RequestTimeoutMs,
                PubFlushMode = PubFlushMode,
                ClientCertificates = new X509Certificate2Collection(ClientCertificates),
                ServerCertificateValidation = ServerCertificateValidation,
                SocketOptions = new SocketOptions
                {
                    AddressType = SocketOptions.AddressType,
                    ReceiveBufferSize = SocketOptions.ReceiveBufferSize,
                    SendBufferSize = SocketOptions.SendBufferSize,
                    ReceiveTimeoutMs = SocketOptions.ReceiveTimeoutMs,
                    SendTimeoutMs = SocketOptions.SendTimeoutMs,
                    ConnectTimeoutMs = SocketOptions.ConnectTimeoutMs,
                    UseNagleAlgorithm = SocketOptions.UseNagleAlgorithm
                }
            };
        }
    }
}