using System;
using System.IO;
using System.Text;
using EnsureThat;
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

        public IPayload Encode<TItem>(TItem item) where TItem : class
        {
            EnsureArg.IsNotNull(item, nameof(item));

            var builder = new PayloadBuilder();

            using (var stream = new MemoryStream())
            {
                using (var sw = new StreamWriter(stream, Encoding.UTF8))
                {
                    var jw = new JsonTextWriter(sw);

                    _serializer.Serialize(jw, item, typeof(TItem));
                }

                builder.Append(stream.ToArray());
            }

            return builder.ToPayload();
        }

        public object Decode(byte[] payload, Type objectType)
        {
            EnsureArg.IsNotNull(objectType, nameof(objectType));

            if (payload == null || payload.Length == 0)
                return null;

            using (var stream = new MemoryStream(payload))
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