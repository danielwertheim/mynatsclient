using System;
using MyNatsClient.Internals;

namespace MyNatsClient
{
    public sealed class SubscriptionInfo : IEquatable<SubscriptionInfo>
    {
        private const char WildCardChar = '*';
        private const char FullWildCardChar = '>';
        private static readonly string[] EmptySubjectParts = new string[0];

        private readonly int _wildCardPos, _fullWildCardPos;
        private readonly bool _matchAllSubjects;
        private readonly string[] _subjectParts;

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
        /// Gets a value indicating if this subscription is a wild card subscription.
        /// </summary>
        public bool HasWildcardSubject { get; }

        /// <summary>
        /// Creates a subscription info object.
        /// </summary>
        /// <param name="subject">The subject name to subscribe to</param>
        /// <param name="queueGroup">If specified, the subscriber will join this queue group</param>
        /// <param name="maxMessages">Number of messages to wait for before automatically unsubscribing</param>
        public SubscriptionInfo(string subject, string queueGroup = null, int? maxMessages = null)
        {
            if(subject == null)
                throw new ArgumentNullException(nameof(subject));

            _wildCardPos = subject.IndexOf(WildCardChar);
            _fullWildCardPos = subject.IndexOf(FullWildCardChar);

            if (_wildCardPos > -1 && _fullWildCardPos > -1)
                throw new ArgumentException("Subject can not contain both the wildcard and full wildcard character.", nameof(subject));

            Id = UniqueId.Generate();
            Subject = subject;
            QueueGroup = queueGroup;
            MaxMessages = maxMessages;

            HasWildcardSubject = _wildCardPos > -1 || _fullWildCardPos > -1;
            _matchAllSubjects = Subject[0] == FullWildCardChar;
            _subjectParts = !_matchAllSubjects && _wildCardPos > -1
                ? subject.Split('.')
                : EmptySubjectParts;
        }

        public bool Matches(string testSubject)
        {
            //EnsureArg.IsNotNullOrWhiteSpace(testSubject, nameof(testSubject));

            if (_matchAllSubjects)
                return true;

            if (!HasWildcardSubject)
                return Subject.Equals(testSubject, StringComparison.Ordinal);

            if (_fullWildCardPos > -1)
            {
                var prefix = Subject.Substring(0, _fullWildCardPos);
                return testSubject.StartsWith(prefix, StringComparison.Ordinal);
            }

            if (_wildCardPos > -1)
            {
                var testParts = testSubject.Split('.');
                if (testParts.Length != _subjectParts.Length)
                    return false;

                for (var i = 0; i < testParts.Length; i++)
                {
                    if (!testParts[i].Equals(_subjectParts[i], StringComparison.Ordinal) && !string.Equals(_subjectParts[i], "*", StringComparison.Ordinal))
                        return false;
                }

                return true;
            }

            throw new Exception("Should not have reached this point.");
        }

        public override int GetHashCode() => Id.GetHashCode();

        public bool Equals(SubscriptionInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Id == other.Id;
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is SubscriptionInfo other && Equals(other);
    }
}