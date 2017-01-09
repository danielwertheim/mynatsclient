using System;

namespace MyNatsClient
{
    public interface IDecoder
    {
        object Decode(byte[] payload, Type objectType);
    }
}