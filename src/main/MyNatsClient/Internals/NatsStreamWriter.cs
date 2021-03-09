using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MyNatsClient.Internals
{
    internal class NatsStreamWriter : INatsStreamWriter
    {
        private readonly Stream _stream;
        private readonly CancellationToken _cancellationToken;

        internal NatsStreamWriter(Stream stream, CancellationToken cancellationToken)
        {
            _stream = stream;
            _cancellationToken = cancellationToken;
        }

        public void Flush()
            => _stream.Flush();

        public async Task FlushAsync()
            => await _stream.FlushAsync(_cancellationToken).ConfigureAwait(false);

        public void Write(ReadOnlySpan<byte> data, bool flush)
        {
            _stream.Write(data);
            if(flush)
                _stream.Flush();
        }

        public async Task WriteAsync(ReadOnlyMemory<byte> data, bool flush)
        {
            await _stream.WriteAsync(data, _cancellationToken).ConfigureAwait(false);
            if(flush)
                await _stream.FlushAsync(_cancellationToken).ConfigureAwait(false);
        }
    }
}