using FluentAssertions;
using MyNatsClient;
using Xunit;

namespace UnitTests.Encodings
{
    public abstract class EncodingTestOf<TEncoding> : UnitTestsOf<TEncoding> where TEncoding : IEncoding
    {
        protected EncodingTestOf(TEncoding encoding)
        {
            UnitUnderTest = encoding;
        }

        [Fact]
        public void Should_be_able_to_Encode_and_Decode_an_instance()
        {
            var testItem = EncodingTestItem.Create();

            var encoded = UnitUnderTest.Encode(testItem);
            encoded.Should().NotBeNullOrEmpty();

            var decoded = UnitUnderTest.Decode(encoded.GetBytes().ToArray(), testItem.GetType());
            decoded.Should().BeEquivalentTo(testItem);
        }

        [Fact]
        public void Should_return_null_When_passing_null_payload()
            => UnitUnderTest.Decode(null, typeof(EncodingTestItem)).Should().BeNull();

        [Fact]
        public void Should_return_null_When_passing_empty_payload()
            => UnitUnderTest.Decode(new byte[0], typeof(EncodingTestItem)).Should().BeNull();
    }
}