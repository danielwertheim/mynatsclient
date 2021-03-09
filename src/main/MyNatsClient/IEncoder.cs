using System;

namespace MyNatsClient
{
    public interface IEncoder
    {
        ReadOnlyMemory<byte> Encode<TItem>(TItem item) where TItem : class;
    }
}