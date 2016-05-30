using System.Text;
using MyNatsClient.Internals;

namespace MyNatsClient.Ops
{
    public class MsgOp : IOp
    {
        public string Code => "MSG";
        public string Subject { get; }
        public string QueueGroup { get; }
        public string SubscriptionId { get; }
        public byte[] Payload { get; }

        private readonly int _size;

        public MsgOp(
            string subject,
            string subscriptionId,
            byte[] payload,
            string queueGroup = null)
        {
            Subject = subject;
            QueueGroup = queueGroup;
            SubscriptionId = subscriptionId;
            Payload = payload;
            _size = 3 + //MSG
                    1 + //BLANK
                    Subject.Length +
                    1 + //BLANK
                    (QueueGroup?.Length + 1 ?? 0) + //Optinal GRP + BLANK
                    SubscriptionId.Length +
                    1 + //BLANK
                    Payload.Length.ToString().Length +
                    2 + //CRLF
                    Payload.Length +
                    2; //CRLF
        }

        public string GetPayloadAsString()
        {
            return NatsEncoder.GetString(Payload);
        }

        public string GetAsString()
        {
            var sb = new StringBuilder(_size);
            sb.Append("MSG");
            sb.Append(" ");
            sb.Append(Subject);
            sb.Append(" ");
            if (QueueGroup != null)
            {
                sb.Append(QueueGroup);
                sb.Append(" ");
            }

            sb.Append(SubscriptionId);
            sb.Append(" ");
            sb.Append(Payload.Length);
            sb.Append("\r\n");
            sb.Append(GetPayloadAsString());

            return sb.ToString();
        }
    }
}