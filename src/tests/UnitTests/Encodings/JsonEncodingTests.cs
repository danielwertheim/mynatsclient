using MyNatsClient.Encodings.Json;

namespace UnitTests.Encodings
{
    public class JsonEncodingTests : EncodingTestOf<JsonEncoding>
    {
        public JsonEncodingTests() : base(JsonEncoding.Default) { }
    }
}