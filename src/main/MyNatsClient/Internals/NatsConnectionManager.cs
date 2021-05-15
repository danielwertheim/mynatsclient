using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyNatsClient.Internals.Commands;
using MyNatsClient.Internals.Extensions;
using MyNatsClient.Ops;

namespace MyNatsClient.Internals
{
    internal class NatsConnectionManager : INatsConnectionManager
    {
        private readonly ILogger<NatsConnectionManager> _logger = LoggerManager.CreateLogger<NatsConnectionManager>();

        private readonly ISocketFactory _socketFactory;

        internal NatsConnectionManager(ISocketFactory socketFactory)
            => _socketFactory = socketFactory ?? throw new ArgumentNullException(nameof(socketFactory));

        public async Task<(INatsConnection connection, IList<IOp> consumedOps)> OpenConnectionAsync(
            ConnectionInfo connectionInfo,
            CancellationToken cancellationToken)
        {
            if (connectionInfo == null)
                throw new ArgumentNullException(nameof(connectionInfo));

            cancellationToken.ThrowIfCancellationRequested();

            var hosts = new Queue<Host>(connectionInfo.Hosts.GetRandomized());

            bool ShouldTryAndConnect() => !cancellationToken.IsCancellationRequested && hosts.Any();

            while (ShouldTryAndConnect())
            {
                var host = hosts.Dequeue();

                try
                {
                    return await EstablishConnectionAsync(
                        host,
                        connectionInfo,
                        cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while connecting to {Host}. Trying with next host (if any).", host);

                    if (!ShouldTryAndConnect())
                        throw;
                }
            }

            throw NatsException.CouldNotEstablishAnyConnection();
        }

        private static bool DefaultServerCertificateValidation(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            => sslPolicyErrors == SslPolicyErrors.None;

        private async Task<(INatsConnection connection, IList<IOp> consumedOps)> EstablishConnectionAsync(
            Host host,
            ConnectionInfo connectionInfo,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Establishing connection to {Host}", host);
            var serverCertificateValidation = connectionInfo.ServerCertificateValidation ?? DefaultServerCertificateValidation;

            bool RemoteCertificateValidationCallback(object _, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
                => serverCertificateValidation(certificate, chain, errors);

            var consumedOps = new List<IOp>();
            Socket socket = null;
            Stream stream = null;
            NatsOpStreamReader reader = null;

            try
            {
                _logger.LogDebug("Creating socket.");
                socket = _socketFactory.Create(connectionInfo.SocketOptions);
                await socket.ConnectAsync(
                    host,
                    connectionInfo.SocketOptions.ConnectTimeoutMs,
                    cancellationToken).ConfigureAwait(false);

                _logger.LogDebug("Creating read write stream.");
                stream = socket.CreateReadWriteStream();
                reader = NatsOpStreamReader.Use(stream);

                _logger.LogDebug("Trying to read InfoOp.");
                var op = reader.ReadOp();
                if (op == null)
                    throw NatsException.FailedToConnectToHost(host,
                        "Expected to get INFO after establishing connection. Got nothing.");

                if (op is not InfoOp infoOp)
                    throw NatsException.FailedToConnectToHost(host,
                        $"Expected to get INFO after establishing connection. Got {op.GetType().Name}.");

                _logger.LogDebug("Parsing server info.");
                var serverInfo = NatsServerInfo.Parse(infoOp.Message);
                var credentials = host.HasNonEmptyCredentials() ? host.Credentials : connectionInfo.Credentials;
                if (serverInfo.AuthRequired && (credentials == null || credentials == Credentials.Empty))
                    throw NatsException.MissingCredentials(host);

                if (serverInfo.TlsVerify && connectionInfo.ClientCertificates.Count == 0)
                    throw NatsException.MissingClientCertificates(host);

                consumedOps.Add(op);

                if (serverInfo.TlsRequired)
                {
                    _logger.LogDebug("Creating SSL Stream.");
                    stream = new SslStream(stream, false, RemoteCertificateValidationCallback, null, EncryptionPolicy.RequireEncryption);
                    var ssl = (SslStream) stream;

                    var clientAuthOptions = new SslClientAuthenticationOptions
                    {
                        RemoteCertificateValidationCallback = RemoteCertificateValidationCallback,
                        AllowRenegotiation = true,
                        CertificateRevocationCheckMode = X509RevocationMode.Online,
                        ClientCertificates = connectionInfo.ClientCertificates,
                        EnabledSslProtocols = SslProtocols.Tls12,
                        EncryptionPolicy = EncryptionPolicy.RequireEncryption,
                        TargetHost = host.Address
                    };

                    _logger.LogDebug("Performing SSL client authentication.");
                    await ssl.AuthenticateAsClientAsync(clientAuthOptions, cancellationToken).ConfigureAwait(false);

                    reader.SetNewSource(ssl);
                }

                _logger.LogDebug("Sending Connect.");
                stream.Write(ConnectCmd.Generate(connectionInfo.Verbose, credentials, connectionInfo.Name));
                _logger.LogDebug("Sending Ping.");
                stream.Write(PingCmd.Bytes.Span);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogDebug("Trying to read OP to see if connection was established.");
                op = reader.ReadOp();
                switch (op)
                {
                    case NullOp:
                        throw NatsException.FailedToConnectToHost(host,
                            "Expected to read something after CONNECT and PING. Got nothing.");
                    case ErrOp:
                        throw NatsException.FailedToConnectToHost(host,
                            $"Expected to get PONG after sending CONNECT and PING. Got {op.Marker}.");
                }

                if (!socket.Connected)
                    throw NatsException.FailedToConnectToHost(host, "No connection could be established.");

                consumedOps.Add(op);

                _logger.LogInformation("Connection successfully established to {Host}", host);

                return (
                    new NatsConnection(
                        serverInfo,
                        socket,
                        stream,
                        cancellationToken),
                    consumedOps);
            }
            catch
            {
                Swallow.Everything(
                    () =>
                    {
                        reader?.Dispose();
                        reader = null;
                    },
                    () =>
                    {
                        stream?.Dispose();
                        stream = null;
                    },
                    () =>
                    {
                        if (socket == null)
                            return;

                        if (socket.Connected)
                            socket.Shutdown(SocketShutdown.Both);

                        socket.Dispose();
                        socket = null;
                    });

                throw;
            }
        }
    }
}
