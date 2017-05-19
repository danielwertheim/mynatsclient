using System;
using FluentAssertions;
using MyNatsClient.Encodings.Json;
using Xunit;

namespace UnitTests.Encodings
{
    public class JsonEncodingTests : UnitTestsOf<JsonEncoding>
    {
        public JsonEncodingTests()
        {
            UnitUnderTest = JsonEncoding.Default;
        }

        [Fact]
        public void Should_be_able_to_Encode_and_Decode_an_instance()
        {
            var testItem = new TestItem
            {
                SomeString = Guid.NewGuid().ToString("N")
            };
            var encoded = UnitUnderTest.Encode(testItem);
            encoded.Should().NotBeNullOrEmpty();

            var decoded = UnitUnderTest.Decode(encoded.GetBytes().ToArray(), testItem.GetType());
            decoded.ShouldBeEquivalentTo(testItem);
        }

        [Fact]
        public void Should_return_null_When_passing_null_payload()
            => UnitUnderTest.Decode(null, typeof(TestItem)).Should().BeNull();

        [Fact]
        public void Should_return_null_When_passing_empty_payload()
            => UnitUnderTest.Decode(new byte[0], typeof(TestItem)).Should().BeNull();

        private class TestItem
        {
            public string SomeString { get; set; }
        }
    }
}