using System;

namespace MyNatsClient.Ops
{
    public sealed class InfoOp : IOp
    {
        internal const string OpMarker = "INFO";

        public string Marker => OpMarker;

        public readonly ReadOnlyMemory<char> Message;

        public InfoOp(ReadOnlySpan<char> message)
            => Message = message.ToArray();

        public override string ToString()
            => OpMarker;
    }
}
