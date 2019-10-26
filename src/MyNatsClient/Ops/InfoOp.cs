using System;

namespace MyNatsClient.Ops
{
    public sealed class InfoOp : IOp
    {
        public const string Name = "INFO";

        public readonly ReadOnlyMemory<char> Message;

        public InfoOp(ReadOnlyMemory<char> message)
            => Message = message;

        public string GetAsString()
            => $"{Name} {Message.Span.ToString()}";
    }
}