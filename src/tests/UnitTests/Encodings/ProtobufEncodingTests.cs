using MyNatsClient.Encodings.Protobuf;

namespace UnitTests.Encodings
{
    public class ProtobufEncodingTests : EncodingTestOf<ProtobufEncoding>
    {
        public ProtobufEncodingTests() : base(ProtobufEncoding.Default) { }
    }
}