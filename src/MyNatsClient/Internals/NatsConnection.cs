using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MyNatsClient.Internals
{
    internal sealed class NatsConnection : INatsConnection
    {
        private readonly Func<bool> _socketIsConnected;
        private readonly Func<bool> _canRead;
        private readonly CancellationToken _cancellationToken;

        private Socket _socket;
        private Stream _stream;
        private BufferedStream _writeStream;
        private BufferedStream _readStream;
        private SemaphoreSlim _writeStreamSync;
        private NatsOpStreamReader _reader;
        private NatsStreamWriter _writer;
        private bool _isDisposed;

        public INatsServerInfo ServerInfo { get; }
        public bool IsConnected => _socketIsConnected();
        public bool CanRead => _canRead();

        internal NatsConnection(
            NatsServerInfo serverInfo,
            Socket socket,
            Stream stream,
            CancellationToken cancellationToken)
        {
            ServerInfo = serverInfo ?? throw new ArgumentNullException(nameof(serverInfo));

            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            if (!socket.Connected)
                throw new ArgumentException("Socket is not connected.", nameof(socket));

            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _writeStream = new BufferedStream(_stream, socket.SendBufferSize);
            _readStream = new BufferedStream(_stream, socket.ReceiveBufferSize);
            _cancellationToken = cancellationToken;
            _writeStreamSync = new SemaphoreSlim(1, 1);
            _writer = new NatsStreamWriter(_writeStream, _cancellationToken);
            _reader = new NatsOpStreamReader(_readStream);

            _socketIsConnected = () => _socket?.Connected == true;
            _canRead = () => _socket?.Connected == true && _stream != null && _stream.CanRead && !_cancellationToken.IsCancellationRequested;
        }

        public void Dispose()
        {
            ThrowIfDisposed();

            _isDisposed = true;

            var exs = new List<Exception>();

            void TryDispose(IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    exs.Add(ex);
                }
            }

            TryDispose(_writeStream);
            TryDispose(_readStream);
            TryDispose(_stream);
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception ex)
            {
                exs.Add(ex);
            }
            TryDispose(_socket);
            TryDispose(_writeStreamSync);

            _writeStream = null;
            _readStream = null;
            _stream = null;
            _socket = null;
            _writeStreamSync = null;
            _reader = null;
            _writer = null;

            if (exs.Any())
                throw new AggregateException("Failed while disposing connection. See inner exception(s) for more details.", exs);
        }

        public IEnumerable<IOp> ReadOp()
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            return _reader.ReadOps();
        }

        public void WithWriteLock(Action<INatsStreamWriter> a)
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            _writeStreamSync.Wait(_cancellationToken);

            try
            {
                a(_writer);
            }
            finally
            {
                _writeStreamSync.Release();
            }
        }

        public void WithWriteLock<TArg>(Action<INatsStreamWriter, TArg> a, TArg arg)
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            _writeStreamSync.Wait(_cancellationToken);

            try
            {
                a(_writer, arg);
            }
            finally
            {
                _writeStreamSync.Release();
            }
        }

        public async Task WithWriteLockAsync(Func<INatsStreamWriter, Task> a)
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            await _writeStreamSync.WaitAsync(_cancellationToken).ConfigureAwait(false);

            try
            {
                await a(_writer).ConfigureAwait(false);
            }
            finally
            {
                _writeStreamSync.Release();
            }
        }

        public async Task WithWriteLockAsync<TArg>(Func<INatsStreamWriter, TArg, Task> a, TArg arg)
        {
            ThrowIfDisposed();

            ThrowIfNotConnected();

            await _writeStreamSync.WaitAsync(_cancellationToken).ConfigureAwait(false);

            try
            {
                await a(_writer, arg).ConfigureAwait(false);
            }
            finally
            {
                _writeStreamSync.Release();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        private void ThrowIfNotConnected()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Connection has been disconnected.");
        }
    }
}
