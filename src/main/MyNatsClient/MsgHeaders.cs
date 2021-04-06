using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MyNatsClient
{
    public class MsgHeaders : IReadOnlyDictionary<string, IImmutableList<string>>
    {
        private readonly ConcurrentDictionary<string, IImmutableList<string>> _state = new();

        public IImmutableList<string> this[string key] => _state[key];

        public IEnumerable<string> Keys => _state.Keys;

        public IEnumerable<IImmutableList<string>> Values => _state.Values;

        public int Count => _state.Count;

        private static void EnsureKeyIsValid(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key is missing.", nameof(key));

            foreach (var c in key)
            {
                if (c < 33 || c > 126 || c == ':' || c == '"' || c == '\'' || c == '?')
                    throw new ArgumentException($"Key contains invalid char '{c}'.", nameof(key));
            }
        }

        private static void EnsureValueIsValid(string value)
        {
            if (value == null)
                throw new ArgumentException("Value can not be null.", nameof(value));

            foreach (var c in value)
            {
                if (c < 32 || c > 126 || c == ':' || c == '"' || c == '\'')
                    throw new ArgumentException($"Value contains invalid char '{c}'.", nameof(value));
            }
        }

        public void Add(string key, string value)
        {
            EnsureKeyIsValid(key);
            EnsureValueIsValid(value);

            _state.AddOrUpdate(
                key,
                _ => ImmutableList.Create(value),
                (_, existingValues) => existingValues.Add(value));
        }

        public void Set(string key, string[] values)
        {
            EnsureKeyIsValid(key);

            foreach (var value in values)
                EnsureValueIsValid(value);

            _state[key] = ImmutableList.Create(values);
        }

        public bool ContainsKey(string key)
            => _state.ContainsKey(key);

        public bool TryGetValue(string key, out IImmutableList<string> values)
            => _state.TryGetValue(key, out values);

        public IEnumerator<KeyValuePair<string, IImmutableList<string>>> GetEnumerator()
            => _state.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _state.GetEnumerator();
    }
}
