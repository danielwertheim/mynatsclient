using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MyNatsClient.Internals.Commands;
using MyNatsClient.Internals.Extensions;
using MyNatsClient.Ops;

namespace MyNatsClient.Internals
{
    internal class NatsConnection : IDisposable
    {
        private const int TryConnectMaxCycleDelayMs = 200;
        private const int TryConnectMaxDurationMs = 2000;

        private static readonly ILogger Logger = LoggerManager.Resolve(typeof(NatsConnection));

        private readonly Func<bool> _socketIsConnected;
        private readonly Func<bool> _canRead;

        private Socket _socket;
        private Stream _readStream;
        private Stream _writeStream;
        private Locker _writeStreamSync;
        private NatsOpStreamReader _reader;
        private NatsStreamWriter _writer;
        private CancellationToken _cancellationToken;
        private bool _isDisposed;

        public NatsServerInfo ServerInfo { get; }
        public bool IsConnected => _socketIsConnected();
        public bool CanRead => _canRead();

        private NatsConnection(
            NatsServerInfo serverInfo,
            Socket socket,
            BufferedStream writeStream,
            BufferedStream readStream,
            NatsOpStreamReader reader,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(serverInfo, nameof(serverInfo));
            EnsureArg.IsNotNull(socket, nameof(socket));
            EnsureArg.IsNotNull(writeStream, nameof(writeStream));
            EnsureArg.IsNotNull(readStream, nameof(readStream));
            EnsureArg.IsNotNull(reader, nameof(reader));

            if (!socket.Connected)
                throw new ArgumentException("Socket is not connected.", nameof(socket));

            ServerInfo = serverInfo;

            _socket = socket;
            _writeStreamSync = new Locker();
            _writeStream = writeStream;
            _readStream = readStream;
            _reader = reader;
            _cancellationToken = cancellationToken;

            _writer = new NatsStreamWriter(_writeStream, ServerInfo.MaxPayload, _cancellationToken);

            _socketIsConnected = () => _socket != null && _socket.Connected;
            _canRead = () => _socketIsConnected() && _readStream != null && _readStream.CanRead && !_cancellationToken.IsCancellationRequested;
        }

        internal static NatsConnection Connect(ConnectionInfo connectionInfo, ISocketFactory socketFactory, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(connectionInfo, nameof(connectionInfo));
            EnsureArg.IsNotNull(socketFactory, nameof(socketFactory));

            var hosts = new Queue<Host>(connectionInfo.Hosts.GetRandomized());
            while (!cancellationToken.IsCancellationRequested && hosts.Any())
            {
                var host = hosts.Dequeue();

                Socket socket = null;
                BufferedStream writeStream = null;
                BufferedStream readStream = null;

                try
                {
                    socket = socketFactory.Create(connectionInfo.SocketOptions);
                    socket.Connect(host.Address, host.Port);

                    writeStream = new BufferedStream(socket.CreateWriteStream(), socket.SendBufferSize);
                    readStream = new BufferedStream(socket.CreateReadStream(), socket.ReceiveBufferSize);
                    var reader = new NatsOpStreamReader(readStream);

                    Func<IOp> readOne = () => Retry.This(() => reader.ReadOp().FirstOrDefault(), TryConnectMaxCycleDelayMs, TryConnectMaxDurationMs);

                    var op = readOne();
                    if (op == null)
                    {
                        Logger.Error($"Error while connecting to {host}. Expected to get INFO after connection. Got nothing.");
                        continue;
                    }

                    var infoOp = op as InfoOp;
                    if (infoOp == null)
                    {
                        Logger.Error($"Error while connecting to {host}. Expected to get INFO after connection. Got {op.GetAsString()}.");
                        continue;
                    }

                    var serverInfo = NatsServerInfo.Parse(infoOp.Message);
                    var credentials = host.HasNonEmptyCredentials() ? host.Credentials : connectionInfo.Credentials;
                    if (serverInfo.AuthRequired && (credentials == null || credentials == Credentials.Empty))
                        throw NatsException.MissingCredentials(host.ToString());

                    if (!VerifyConnectedOk(host, socket, readOne, ref op))
                        continue;

                    return new NatsConnection(
                        serverInfo,
                        socket,
                        writeStream,
                        readStream,
                        reader,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error while connecting to {host}.", ex);
                    Swallow.Everything(() =>
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
                }
            }

            Logger.Error("No connection could be made to any of the specified NATS hosts.");

            throw NatsException.NoConnectionCouldBeMade();
        }

        private static bool VerifyConnectedOk(Host host, Socket socket, Func<IOp> readOne, ref IOp op)
        {
            if (!socket.Connected)
            {
                Logger.Error($"Error while connecting to {host}. No connection could be established.");
                return false;
            }

            socket.Send(PingCmd.Generate());

            op = readOne();
            if (op == null)
            {
                Logger.Error($"Error while connecting to {host}. Expected to get INFO after connection. Got nothing.");
                return false;
            }

            if (op is ErrOp)
            {
                Logger.Error($"Error while connecting to {host}. Expected to get PONG after sending CONNECT and PING. Got {op.GetAsString()}.");
                return false;
            }

            if (socket.Connected)
                return true;

            Logger.Error($"Error while connecting to {host}. No connection could be established.");

            return false;
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            Dispose(true);
            GC.SuppressFinalize(this);
            _isDisposed = true;
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed || !disposing)
                return;

            Swallow.Everything(
                () =>
                {
#if !NETSTANDARD1_6
                    _readStream?.Close();
#endif
                    _readStream?.Dispose();
                    _readStream = null;
                },
                () =>
                {
#if !NETSTANDARD1_6
                    _writeStream?.Close();
#endif
                    _writeStream?.Dispose();
                    _writeStream = null;
                },
                () =>
                {
                    _socket?.Shutdown(SocketShutdown.Both);
#if !NETSTANDARD1_6
                    _socket?.Close();
#endif
                    //_socket?.Disconnect(false);
                    _socket?.Dispose();
                    _socket = null;
                },
                () =>
                {
                    _writeStreamSync?.Dispose();
                    _writeStreamSync = null;
                });
        }

        public IEnumerable<IOp> ReadOp()
        {
            //ThrowIfDisposed();

            //ThrowIfNotConnected();

            return _reader.ReadOp();
        }

        public void WithWriteLock(Action<INatsStreamWriter> a)
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            using (_writeStreamSync.Lock())
                a(_writer);
        }

        public async Task WithWriteLockAsync(Func<INatsStreamWriter, Task> a)
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            using (await _writeStreamSync.LockAsync(_cancellationToken).ForAwait())
                await a(_writer).ForAwait();
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        private void ThrowIfNotConnected()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Can not send. Connection has been disconnected.");
        }
    }
}