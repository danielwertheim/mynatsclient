using System;
using System.Text;
using MyNatsClient.Internals;

namespace MyNatsClient.Ops
{
    public sealed class MsgOp : IOp
    {
        public const string Name = "MSG";

        public readonly string Subject;
        public readonly string ReplyTo;
        public readonly string SubscriptionId;
        public readonly ReadOnlyMemory<byte> Payload;

        public MsgOp(
            ReadOnlySpan<char> subject,
            ReadOnlySpan<char> subscriptionId,
            ReadOnlySpan<char> replyTo,
            ReadOnlyMemory<byte> payload)
        {
            Subject = subject.ToString();
            SubscriptionId = subscriptionId.ToString();
            ReplyTo = replyTo.ToString();
            Payload = payload;
        }

        public string GetPayloadAsString()
            => NatsEncoder.GetString(Payload.Span);

        public string GetAsString()
        {
            var sb = new StringBuilder();
            sb.Append(Name);
            sb.Append(" ");
            sb.Append(Subject);
            sb.Append(" ");
            if (ReplyTo != string.Empty)
            {
                sb.Append(ReplyTo);
                sb.Append(" ");
            }

            sb.Append(SubscriptionId);
            sb.Append(" ");
            sb.Append(Payload.Length);
            sb.Append(Environment.NewLine);
            sb.Append(GetPayloadAsString());

            return sb.ToString();
        }
    }
}