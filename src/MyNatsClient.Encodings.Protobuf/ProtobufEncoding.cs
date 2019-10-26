using System;
using System.IO;
using ProtoBuf;

namespace MyNatsClient.Encodings.Protobuf
{
    public class ProtobufEncoding : IEncoding
    {
        public static ProtobufEncoding Default { get; set; } = new ProtobufEncoding();

        public ReadOnlyMemory<byte> Encode<TItem>(TItem item) where TItem : class
        {
            if(item == null)
                return ReadOnlyMemory<byte>.Empty;

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, item);

                return stream.ToArray();
            }
        }

        public object Decode(ReadOnlySpan<byte> payload, Type objectType)
        {
            if(objectType == null)
                throw new ArgumentNullException(nameof(objectType));

            if (payload == null || payload.Length == 0)
                return null;

            using(var stream = new MemoryStream(payload.ToArray(), false))
                return Serializer.Deserialize(objectType, stream);
        }
    }
}