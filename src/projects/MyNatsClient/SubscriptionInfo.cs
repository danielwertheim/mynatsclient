using System;
using EnsureThat;

namespace MyNatsClient
{
    public class SubscriptionInfo
    {
        public string Id { get; }

        /// <summary>
        /// Gets the subject name the subscriber is subscribed to.
        /// </summary>
        public string Subject { get; }

        /// <summary>
        /// Gets the optionally specified queue group that the subscriber will join.
        /// </summary>
        public string QueueGroup { get; }

        /// <summary>
        /// Gets the number of messages the subscriber will wait before automatically unsubscribing.
        /// </summary>
        public int? MaxMessages { get; }

        /// <summary>
        /// Creates a subscription info object.
        /// </summary>
        /// <param name="subject">The subject name to subscribe to</param>
        /// <param name="queueGroup">If specified, the subscriber will join this queue group</param>
        /// <param name="maxMessages">Number of messages to wait for before automatically unsubscribing</param>
        public SubscriptionInfo(string subject, string queueGroup = null, int? maxMessages = null)
        {
            EnsureArg.IsNotNullOrWhiteSpace(subject, nameof(subject));

            Id = Guid.NewGuid().ToString("N");
            Subject = subject;
            QueueGroup = queueGroup;
            MaxMessages = maxMessages;
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