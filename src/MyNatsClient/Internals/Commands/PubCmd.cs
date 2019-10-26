using System;
using System.Threading.Tasks;

namespace MyNatsClient.Internals.Commands
{
    internal static class PubCmd
    {
        private static readonly byte[] Cmd = { (byte)'P', (byte)'U', (byte)'B' };

        internal static void Write(INatsStreamWriter writer, ReadOnlySpan<char> subject, ReadOnlySpan<char> replyTo, ReadOnlyMemory<byte> body)
        {
            var bodySize = body.Length.ToString().AsSpan();
            var preBodySize = 3 + 1 + subject.Length + 1 + (replyTo.IsEmpty ? 0 : replyTo.Length + 1) + bodySize.Length + NatsEncoder.CrlfBytesLen;
            var preBody = new Span<byte>(new byte[preBodySize]);
            
            FillPreBody(preBody, subject, replyTo, bodySize);

            writer.Write(preBody, false);
            writer.Write(body.Span, false);
            writer.Write(NatsEncoder.CrlfBytes, false);
        }

        internal static async Task WriteAsync(INatsStreamWriter writer, ReadOnlyMemory<char> subject, ReadOnlyMemory<char> replyTo, ReadOnlyMemory<byte> body)
        {
            var bodySize = body.Length.ToString().AsMemory();
            var preBodySize = 3 + 1 + subject.Length + 1 + (replyTo.Length > 0 ? replyTo.Length + 1 : 0) + bodySize.Length + NatsEncoder.CrlfBytesLen;
            var preBody = new Memory<byte>(new byte[preBodySize]);
            
            FillPreBody(preBody.Span, subject.Span, replyTo.Span, bodySize.Span);

            await writer.WriteAsync(preBody, false).ConfigureAwait(false);
            await writer.WriteAsync(body, false).ConfigureAwait(false);
            await writer.WriteAsync(NatsEncoder.CrlfBytes, false).ConfigureAwait(false);
        }

        private static void FillPreBody(Span<byte> trg, ReadOnlySpan<char> subject, ReadOnlySpan<char> replyTo, ReadOnlySpan<char> bodySize)
        {
            trg[0] = Cmd[0];
            trg[1] = Cmd[1];
            trg[2] = Cmd[2];
            trg[3] = NatsEncoder.SpaceByte;

            var nextSlot = 4;
            nextSlot = NatsEncoder.WriteSingleByteChars(trg, nextSlot, subject);
            trg[nextSlot++] = NatsEncoder.SpaceByte;

            if (!replyTo.IsEmpty)
            {
                nextSlot = NatsEncoder.WriteSingleByteChars(trg, nextSlot, replyTo);
                trg[nextSlot++] = NatsEncoder.SpaceByte;
            }

            nextSlot = NatsEncoder.WriteSingleByteChars(trg, nextSlot, bodySize);

            NatsEncoder.WriteCrlf(trg, nextSlot);
        }
    }
}