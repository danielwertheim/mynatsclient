using System;

namespace MyNatsClient
{
    public interface IDecoder
    {
        object Decode(ReadOnlySpan<byte> payload, Type objectType);
    }
}