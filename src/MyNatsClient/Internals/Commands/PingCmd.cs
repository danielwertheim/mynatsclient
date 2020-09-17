using System;
using System.Threading.Tasks;

namespace MyNatsClient.Internals.Commands
{
    internal static class PingCmd
    {
        internal static readonly ReadOnlyMemory<byte> Bytes = NatsEncoder.GetBytes($"PING{NatsEncoder.Crlf}");

        internal static void Write(INatsStreamWriter writer)
            => writer.Write(Bytes.Span, true);

        internal static Task WriteAsync(INatsStreamWriter writer)
            => writer.WriteAsync(Bytes, true);
    }
}