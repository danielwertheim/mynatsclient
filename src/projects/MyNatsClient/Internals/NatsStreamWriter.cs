using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MyNatsClient.Internals.Extensions;

namespace MyNatsClient.Internals
{
    internal class NatsStreamWriter : INatsStreamWriter
    {
        private readonly Stream _stream;
        private readonly long _maxPayloadSize;
        private readonly CancellationToken _cancellationToken;

        internal NatsStreamWriter(Stream stream, long maxPayloadSize, CancellationToken cancellationToken)
        {
            _stream = stream;
            _maxPayloadSize = maxPayloadSize;
            _cancellationToken = cancellationToken;
        }

        public void Flush()
        {
            _stream.Flush();
        }

        public async Task FlushAsync()
        {
            await _stream.FlushAsync(_cancellationToken).ForAwait();
        }

        public void Write(byte[] data)
        {
            if (data.Length > _maxPayloadSize)
                throw NatsException.ExceededMaxPayload(_maxPayloadSize, data.Length);

            _stream.Write(data, 0, data.Length);
        }

        public void Write(IPayload data)
        {
            if (data.Size > _maxPayloadSize)
                throw NatsException.ExceededMaxPayload(_maxPayloadSize, data.Size);

            for (var i = 0; i < data.BlockCount; i++)
                _stream.Write(data[i], 0, data[i].Length);
        }

        public async Task WriteAsync(byte[] data)
        {
            if (data.Length > _maxPayloadSize)
                throw NatsException.ExceededMaxPayload(_maxPayloadSize, data.Length);

            await _stream.WriteAsync(data, 0, data.Length, _cancellationToken).ForAwait();
        }

        public async Task WriteAsync(IPayload data)
        {
            if (data.Size > _maxPayloadSize)
                throw NatsException.ExceededMaxPayload(_maxPayloadSize, data.Size);

            for (var i = 0; i < data.BlockCount; i++)
                await _stream.WriteAsync(data[i], 0, data.Size, _cancellationToken).ForAwait();
        }
    }
}