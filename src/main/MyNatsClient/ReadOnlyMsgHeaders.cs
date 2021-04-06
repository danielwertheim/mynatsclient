using System;
using System.Collections;
using System.Collections.Generic;

namespace MyNatsClient
{
    public class ReadOnlyMsgHeaders : IReadOnlyDictionary<string, IReadOnlyList<string>>
    {
        public static readonly ReadOnlyMsgHeaders Empty = new (string.Empty, new Dictionary<string, IReadOnlyList<string>>(0));

        private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _keyValues;

        public string Protocol { get; }
        public IReadOnlyList<string> this[string key] => _keyValues[key];
        public IEnumerable<string> Keys => _keyValues.Keys;
        public IEnumerable<IReadOnlyList<string>> Values => _keyValues.Values;
        public int Count => _keyValues.Count;

        private ReadOnlyMsgHeaders(string protocol, IReadOnlyDictionary<string, IReadOnlyList<string>> keyValues)
        {
            Protocol = protocol;
            _keyValues = keyValues;
        }

        public static ReadOnlyMsgHeaders Create(ReadOnlySpan<char> protocol, IReadOnlyDictionary<string, IReadOnlyList<string>> keyValues)
        {
            if (!protocol.StartsWith("NATS/"))
                throw new ArgumentException("Protocol must start with 'NATS/'.", nameof(protocol));

            return new ReadOnlyMsgHeaders(protocol.ToString(), keyValues);
        }

        public IEnumerator<KeyValuePair<string, IReadOnlyList<string>>> GetEnumerator()
            => _keyValues.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _keyValues.GetEnumerator();

        public bool ContainsKey(string key)
            => _keyValues.ContainsKey(key);

        public bool TryGetValue(string key, out IReadOnlyList<string> value)
            => _keyValues.TryGetValue(key, out value);
    }
}
