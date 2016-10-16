using System;
using EnsureThat;

namespace MyNatsClient
{
    public class SubscriptionInfo
    {
        public string Id { get; }
        public string Subject { get; }
        public string QueueGroup { get; }

        public SubscriptionInfo(string subject, string queueGroup = null)
        {
            EnsureArg.IsNotNullOrWhiteSpace(subject, nameof(subject));

            Id = Guid.NewGuid().ToString("N");
            Subject = subject;
            QueueGroup = queueGroup;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            return Equals(obj as SubscriptionInfo);
        }

        protected bool Equals(SubscriptionInfo other) => string.Equals(Id, other?.Id);

        public override int GetHashCode() => Id.GetHashCode();
    }
}