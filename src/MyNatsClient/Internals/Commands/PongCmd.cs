using System;
using System.Threading.Tasks;

namespace MyNatsClient.Internals.Commands
{
    internal static class PongCmd
    {
        private static readonly ReadOnlyMemory<byte> Bytes = NatsEncoder.GetBytes($"PONG{NatsEncoder.Crlf}");

        internal static void Write(INatsStreamWriter writer)
            => writer.Write(Bytes.Span, true);

        internal static Task WriteAsync(INatsStreamWriter writer)
            => writer.WriteAsync(Bytes, true);
    }
}