using System;
using MyNatsClient.Internals;

namespace MyNatsClient.Ops
{
    public sealed class MsgOp : IOp
    {
        private const string MarkerWithoutHeaders = "MSG";
        private const string MarkerWithHeaders = "HMSG";

        public string Marker { get; }

        public readonly string Subject;
        public readonly string ReplyTo;
        public readonly string SubscriptionId;
        public readonly ReadOnlyMsgHeaders Headers;
        public readonly ReadOnlyMemory<byte> Payload;

        private MsgOp(
            string marker,
            ReadOnlySpan<char> subject,
            ReadOnlySpan<char> subscriptionId,
            ReadOnlySpan<char> replyTo,
            ReadOnlyMsgHeaders headers,
            ReadOnlySpan<byte> payload)
        {
            Marker = marker;
            Subject = subject.ToString();
            SubscriptionId = subscriptionId.ToString();
            ReplyTo = replyTo.ToString();
            Headers = headers;
            Payload = payload.ToArray();
        }

        public static MsgOp CreateMsg(
            ReadOnlySpan<char> subject,
            ReadOnlySpan<char> subscriptionId,
            ReadOnlySpan<char> replyTo,
            ReadOnlySpan<byte> payload) => new(MarkerWithoutHeaders, subject, subscriptionId, replyTo, ReadOnlyMsgHeaders.Empty, payload);

        public static MsgOp CreateHMsg(
            ReadOnlySpan<char> subject,
            ReadOnlySpan<char> subscriptionId,
            ReadOnlySpan<char> replyTo,
            ReadOnlyMsgHeaders headers,
            ReadOnlySpan<byte> payload) => new(MarkerWithHeaders, subject, subscriptionId, replyTo, headers, payload);

        internal static string GetMarker(bool hasHeaders)
            => hasHeaders ? MarkerWithHeaders : MarkerWithoutHeaders;

        public string GetPayloadAsString()
            => NatsEncoder.GetString(Payload.Span);

        public override string ToString()
            => Marker;
    }
}
