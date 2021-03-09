using System;
using System.Threading.Tasks;

namespace MyNatsClient.Internals.Commands
{
    internal static class UnsubCmd
    {
        private static readonly byte[] Cmd = { (byte)'U', (byte)'N', (byte)'S', (byte)'U', (byte)'B' };

        internal static void Write(INatsStreamWriter writer, ReadOnlySpan<char> subscriptionId, int? maxMessages = null)
        {
            var maxMessagesString = maxMessages.ToString().AsSpan();
            var trg = new Span<byte>(new byte[5 + 1 + subscriptionId.Length + (maxMessagesString.IsEmpty ? 0 : maxMessagesString.Length + 1) + NatsEncoder.CrlfBytesLen]);
            
            Fill(trg, subscriptionId, maxMessagesString);

            writer.Write(trg, false);
        }

        internal static async Task WriteAsync(INatsStreamWriter writer, ReadOnlyMemory<char> subscriptionId, int? maxMessages = null)
        {
            var maxMessagesString = maxMessages.ToString().AsMemory();
            var trg = new Memory<byte>(new byte[5 + 1 + subscriptionId.Length + (maxMessagesString.IsEmpty ? 0 : maxMessagesString.Length + 1) + NatsEncoder.CrlfBytesLen]);

            Fill(trg.Span, subscriptionId.Span, maxMessagesString.Span);
            
            await writer.WriteAsync(trg, false).ConfigureAwait(false);
        }

        private static void Fill(Span<byte> trg, ReadOnlySpan<char> subscriptionId, ReadOnlySpan<char> maxMessagesString)
        {
            trg[0] = Cmd[0];
            trg[1] = Cmd[1];
            trg[2] = Cmd[2];
            trg[3] = Cmd[3];
            trg[4] = Cmd[4];
            trg[5] = NatsEncoder.SpaceByte;

            var nextSlot = 6;
            nextSlot = NatsEncoder.WriteSingleByteChars(trg, nextSlot, subscriptionId);
            
            if (!maxMessagesString.IsEmpty)
            {
                trg[nextSlot++] = NatsEncoder.SpaceByte;
                nextSlot = NatsEncoder.WriteSingleByteChars(trg, nextSlot, maxMessagesString);
            }

            NatsEncoder.WriteCrlf(trg, nextSlot);
        }
    }
}