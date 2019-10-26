using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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

        public async Task<Tuple<INatsConnection, IList<IOp>>> OpenConnectionAsync(ConnectionInfo connectionInfo, CancellationToken cancellationToken)
        {
            if(connectionInfo == null)
                throw new ArgumentNullException(nameof(connectionInfo));

            if (cancellationToken.IsCancellationRequested)
                throw NatsException.CouldNotEstablishAnyConnection();

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

        private async Task<Tuple<INatsConnection, IList<IOp>>> EstablishConnectionAsync(Host host, ConnectionInfo connectionInfo, CancellationToken cancellationToken)
        {
            Socket socket = null;
            BufferedStream writeStream = null;
            BufferedStream readStream = null;

            try
            {
                socket = _socketFactory.Create(connectionInfo.SocketOptions);

                await socket.ConnectAsync(
                    host,
                    connectionInfo.SocketOptions.ConnectTimeoutMs,
                    cancellationToken).ConfigureAwait(false);

                readStream = new BufferedStream(socket.CreateReadStream(), socket.ReceiveBufferSize);

                var reader = new NatsOpStreamReader(readStream);
                var (natsServerInfo, consumedOps) = VerifyConnection(host, connectionInfo, socket, reader);

                writeStream = new BufferedStream(socket.CreateWriteStream(), socket.SendBufferSize);

                return new Tuple<INatsConnection, IList<IOp>>(new NatsConnection(
                    natsServerInfo,
                    socket,
                    writeStream,
                    readStream,
                    reader,
                    cancellationToken), consumedOps);
            }
            catch
            {
                Swallow.Everything(
                        () =>
                        {
                            readStream?.Dispose();
                            readStream = null;
                        },
                        () =>
                        {
                            writeStream?.Dispose();
                            writeStream = null;
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

        private static Tuple<NatsServerInfo, IList<IOp>> VerifyConnection(Host host, ConnectionInfo connectionInfo, Socket socket, NatsOpStreamReader opReader)
        {
            if (!socket.Connected)
                throw NatsException.FailedToConnectToHost(host, "Socket was not connected.");

            NatsServerInfo serverInfo = null;
            var consumedOps = new List<IOp>();

            foreach (var op in opReader.ReadOp())
            {
                if (consumedOps.Count == 0)
                {
                    if (op == null)
                        throw NatsException.FailedToConnectToHost(host, "Expected to get INFO after establishing connection. Got nothing.");

                    if (!(op is InfoOp infoOp))
                        throw NatsException.FailedToConnectToHost(host, $"Expected to get INFO after establishing connection. Got {op.GetType().Name}.");

                    serverInfo = NatsServerInfo.Parse(infoOp.Message);
                    var credentials = host.HasNonEmptyCredentials() ? host.Credentials : connectionInfo.Credentials;
                    if (serverInfo.AuthRequired && (credentials == null || credentials == Credentials.Empty))
                        throw NatsException.MissingCredentials(host);

                    socket.Send(ConnectCmd.Generate(connectionInfo.Verbose, credentials));
                    socket.Send(PingCmd.Bytes.Span);
                }
                else if (consumedOps.Count == 1)
                {
                    if (op == null)
                        throw NatsException.FailedToConnectToHost(host, "Expected to read something after CONNECT and PING. Got nothing.");

                    if (op is ErrOp)
                        throw NatsException.FailedToConnectToHost(host, $"Expected to get PONG after sending CONNECT and PING. Got {op.GetAsString()}.");

                    if (!socket.Connected)
                        throw NatsException.FailedToConnectToHost(host, "No connection could be established.");

                    break;
                }
                else
                    throw NatsException.FailedToConnectToHost(host, "Read more than two OPs during connect.");


                consumedOps.Add(op);
            }

            return new Tuple<NatsServerInfo, IList<IOp>>(serverInfo, consumedOps);
        }
    }
}