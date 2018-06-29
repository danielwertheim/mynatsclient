using System;
using System.IO;
using System.Linq;
using EnsureThat;
using ProtoBuf;

namespace MyNatsClient.Encodings.Protobuf
{
    public class ProtobufEncoding : IEncoding
    {
        public static ProtobufEncoding Default { get; set; } = new ProtobufEncoding();

        public IPayload Encode<TItem>(TItem item) where TItem : class
        {
            EnsureArg.IsNotNull(item, nameof(item));

            var builder = new PayloadBuilder();

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, item);

                builder.Append(stream.ToArray());
            }

            return builder.ToPayload();
        }

        public object Decode(byte[] payload, Type objectType)
        {
            EnsureArg.IsNotNull(objectType, nameof(objectType));

            if (payload == null || payload.Length == 0)
                return null;

            using(var stream = new MemoryStream(payload, false))
                return Serializer.Deserialize(objectType, stream);
        }
    }
}