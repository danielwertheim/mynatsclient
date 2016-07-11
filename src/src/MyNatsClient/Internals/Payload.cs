using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MyNatsClient.Internals
{
    internal class Payload : IPayload
    {
        private readonly IReadOnlyList<byte[]> _blocks;

        public byte[] this[int index] => _blocks[index];
        public int BlockCount => _blocks.Count;
        public int Size { get; }
        public IEnumerable<byte[]> Blocks => _blocks;

        internal Payload(IReadOnlyList<byte[]> blocks)
        {
            _blocks = blocks;
            Size += blocks.Sum(b => b.Length);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<byte> GetEnumerator()
        {
            return _blocks.SelectMany(b => b).GetEnumerator();
        }
    }
}