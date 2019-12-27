using System;
using System.Threading.Tasks;

namespace MyNatsClient
{
    public interface INatsStreamWriter
    {
        void Flush();
        Task FlushAsync();
        void Write(ReadOnlySpan<byte> data, bool flush);
        Task WriteAsync(ReadOnlyMemory<byte> data, bool flush);
    }
}