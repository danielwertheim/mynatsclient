using System.Collections.Generic;
using System.Linq;
using MyNatsClient.Internals;

namespace MyNatsClient
{
    /// <summary>
    /// Helps to build payloads which will be batched
    /// into pre-defined block sizes of <see cref="BlockSize"/>.
    /// The <see cref="IPayload"/> being built, is intended
    /// to represent larger messages.
    /// </summary>
    public class PayloadBuilder
    {
        public const byte BlockSize = 128;
        private List<byte[]> _payload;
        private List<byte> _block;

        public PayloadBuilder()
        {
            Reset();
        }

        public void Reset()
        {
            _payload = new List<byte[]>();
            _block = new List<byte>(BlockSize);
        }

        public void Append(byte data)
        {
            _block.Add(data);
            if (_block.Count < BlockSize)
                return;

            Flush();
        }

        public void Append(IPayload data)
        {
            Flush();

            _payload.AddRange(data.Blocks);
        }

        public void Append(byte[] bytes)
        {
            var copied = 0;
            while (copied < bytes.Length)
            {
                var spaceLeft = BlockSize - _block.Count;
                var toCopy = bytes.Length - copied;
                if (toCopy > spaceLeft)
                    toCopy = spaceLeft;
                _block.AddRange(bytes.Skip(copied).Take(toCopy));
                copied += toCopy;
                Flush();
            }
        }

        public IPayload ToPayload()
        {
            Flush();

            var payload = new Payload(_payload.AsReadOnly());
            _payload = new List<byte[]>();

            return payload;
        }

        private void Flush()
        {
            if (_block.Count == 0)
                return;

            _payload.Add(_block.ToArray());
            _block = new List<byte>(BlockSize);
        }
    }
}