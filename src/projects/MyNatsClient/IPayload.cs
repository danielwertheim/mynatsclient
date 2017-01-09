using System.Collections.Generic;

namespace MyNatsClient
{
    public interface IPayload : IEnumerable<byte>
    {
        byte[] this[int index] { get; }
        bool IsEmpty { get; }
        int BlockCount { get; }
        int Size { get; }
        IEnumerable<byte[]> Blocks { get; }
        List<byte> GetBytes();
    }
}