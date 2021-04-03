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
using MyNatsClient.Internals.Commands;
using MyNatsClient.Internals.Extensions;
using MyNatsClient.Ops;

namespace MyNatsClient.Internals
{
    internal class NatsConnectionManager : INatsConnectionManager
    {
        private static readonly ILogger Logger = LoggerManager.Resolve(typeof(NatsConnectionManager));

        private readonly ISocketFactory _socketFactory;

        internal NatsConnectionManager(ISocketFactory socketFactory)
        {
            _socketFactory = socketFactory ?? throw new ArgumentNullException(nameof(socketFactory));
        }

        public async Task<(INatsConnection connection, IList<IOp> consumedOps)> OpenConnectionAsync(
            ConnectionInfo connectionInfo,
            CancellationToken cancellationToken)
        {
            if (connectionInfo == null)
                throw new ArgumentNullException(nameof(connectionInfo));

            cancellationToken.ThrowIfCancellationRequested();

            var hosts = new Queue<Host>(connectionInfo.Hosts.GetRandomized()); //TODO: Rank

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
                    Logger.Error($"Error while connecting to {host}. Trying with next host (if any).", ex);

                    if (!ShouldTryAndConnect())
                        throw;
                }
            }

            throw NatsException.CouldNotEstablishAnyConnection();
        }

        private static bool DefaultServerCertificateValidation(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
            => sslpolicyerrors == SslPolicyErrors.None;

        private async Task<(INatsConnection connection, IList<IOp> consumedOps)> EstablishConnectionAsync(
            Host host,
            ConnectionInfo connectionInfo,
            CancellationToken cancellationToken)
        {
            var serverCertificateValidation = connectionInfo.ServerCertificateValidation ?? DefaultServerCertificateValidation;

            bool RemoteCertificateValidationCallback(object _, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
                => serverCertificateValidation(certificate, chain, errors);

            var consumedOps = new List<IOp>();
            Socket socket = null;
            Stream stream = null;

            try
            {
                socket = _socketFactory.Create(connectionInfo.SocketOptions);
                await socket.ConnectAsync(
                    host,
                    connectionInfo.SocketOptions.ConnectTimeoutMs,
                    cancellationToken).ConfigureAwait(false);

                stream = socket.CreateReadWriteStream();
                var reader = new NatsOpStreamReader(stream);

                var op = reader.ReadOneOp();
                if (op == null)
                    throw NatsException.FailedToConnectToHost(host,
                        "Expected to get INFO after establishing connection. Got nothing.");

                if (!(op is InfoOp infoOp))
                    throw NatsException.FailedToConnectToHost(host,
                        $"Expected to get INFO after establishing connection. Got {op.GetType().Name}.");

                var serverInfo = NatsServerInfo.Parse(infoOp.Message);
                var credentials = host.HasNonEmptyCredentials() ? host.Credentials : connectionInfo.Credentials;
                if (serverInfo.AuthRequired && (credentials == null || credentials == Credentials.Empty))
                    throw NatsException.MissingCredentials(host);

                if (serverInfo.TlsVerify && connectionInfo.ClientCertificates.Count == 0)
                    throw NatsException.MissingClientCertificates(host);

                consumedOps.Add(op);

                if (serverInfo.TlsRequired)
                {
                    await stream.DisposeAsync();
                    stream = new SslStream(socket.CreateReadWriteStream(), false, RemoteCertificateValidationCallback, null, EncryptionPolicy.RequireEncryption);
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

                    await ssl.AuthenticateAsClientAsync(clientAuthOptions, cancellationToken).ConfigureAwait(false);

                    reader = new NatsOpStreamReader(ssl);
                }

                stream.Write(ConnectCmd.Generate(connectionInfo.Verbose, credentials, connectionInfo.Name));
                stream.Write(PingCmd.Bytes.Span);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

                op = reader.ReadOneOp();
                if (op == null)
                    throw NatsException.FailedToConnectToHost(host,
                        "Expected to read something after CONNECT and PING. Got nothing.");

                if (op is ErrOp)
                    throw NatsException.FailedToConnectToHost(host,
                        $"Expected to get PONG after sending CONNECT and PING. Got {op.Marker}.");

                if (!socket.Connected)
                    throw NatsException.FailedToConnectToHost(host, "No connection could be established.");

                consumedOps.Add(op);

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
