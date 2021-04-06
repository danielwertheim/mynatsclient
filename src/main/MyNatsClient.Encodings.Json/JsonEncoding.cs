using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace MyNatsClient.Encodings.Json
{
    public class JsonEncoding : IEncoding
    {
        private readonly JsonSerializer _serializer;

        public static JsonEncoding Default { get; set; } = new JsonEncoding();

        public JsonEncoding(JsonSerializerSettings settings = null)
        {
            _serializer = JsonSerializer.Create(settings ?? JsonSettings.Create());
        }

        public ReadOnlyMemory<byte> Encode<TItem>(TItem item) where TItem : class
        {
            if(item == null)
                return ReadOnlyMemory<byte>.Empty;

            using (var stream = new MemoryStream())
            {
                using (var sw = new StreamWriter(stream, Encoding.UTF8))
                {
                    var jw = new JsonTextWriter(sw);

                    _serializer.Serialize(jw, item, typeof(TItem));
                }

                return stream.ToArray();
            }
        }

        public object Decode(ReadOnlySpan<byte> payload, Type objectType)
        {
            if(objectType == null)
                throw new ArgumentNullException(nameof(objectType));

            if (payload == null || payload.Length == 0)
                return null;

            using (var stream = new MemoryStream(payload.ToArray(), false))
            {
                using (var sr = new StreamReader(stream, Encoding.UTF8))
                {
                    var jr = new JsonTextReader(sr);

                    return _serializer.Deserialize(jr, objectType);
                }
            }
        }
    }
}
