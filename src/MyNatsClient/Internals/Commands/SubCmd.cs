using System;
using System.Threading.Tasks;

namespace MyNatsClient.Internals.Commands
{
    internal static class SubCmd
    {
        private static readonly byte[] Cmd = { (byte)'S', (byte)'U', (byte)'B' };

        internal static void Write(INatsStreamWriter writer, ReadOnlySpan<char> subject, ReadOnlySpan<char> subscriptionId, ReadOnlySpan<char> queueGroup)
        {
            var trg = new Span<byte>(new byte[3 + 1 + subject.Length + 1 + (queueGroup.IsEmpty ? 0 : queueGroup.Length + 1) + subscriptionId.Length + NatsEncoder.CrlfBytesLen]);

            Fill(trg, subject, subscriptionId, queueGroup);

            writer.Write(trg, false);
        }

        internal static async Task WriteAsync(INatsStreamWriter writer, ReadOnlyMemory<char> subject, ReadOnlyMemory<char> subscriptionId, ReadOnlyMemory<char> queueGroup)
        {
            var trg = new Memory<byte>(new byte[3 + 1 + subject.Length + 1 + (queueGroup.IsEmpty ? 0 : queueGroup.Length + 1) + subscriptionId.Length + NatsEncoder.CrlfBytesLen]);

            Fill(trg.Span, subject.Span, subscriptionId.Span, queueGroup.Span);

            await writer.WriteAsync(trg, false).ConfigureAwait(false);
        }

        private static void Fill(Span<byte> trg, ReadOnlySpan<char> subject, ReadOnlySpan<char> subscriptionId, ReadOnlySpan<char> queueGroup)
        {
            trg[0] = Cmd[0];
            trg[1] = Cmd[1];
            trg[2] = Cmd[2];
            trg[3] = NatsEncoder.SpaceByte;

            var nextSlot = 4;
            nextSlot = NatsEncoder.WriteSingleByteChars(trg, nextSlot, subject);
            trg[nextSlot++] = NatsEncoder.SpaceByte;

            if (!queueGroup.IsEmpty)
            {
                nextSlot = NatsEncoder.WriteSingleByteChars(trg, nextSlot, queueGroup);
                trg[nextSlot++] = NatsEncoder.SpaceByte;
            }

            nextSlot = NatsEncoder.WriteSingleByteChars(trg, nextSlot, subscriptionId);
            NatsEncoder.WriteCrlf(trg, nextSlot);
        }
    }
}