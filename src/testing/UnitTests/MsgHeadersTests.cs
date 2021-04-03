using System;
using System.Linq;
using FluentAssertions;
using MyNatsClient;
using Xunit;

namespace UnitTests
{
    public class MsgHeadersTests : UnitTestsOf<MsgHeaders>
    {
        private string NewValidKey() => Guid.NewGuid().ToString("N");
        private string NewValidValue() => Guid.NewGuid().ToString("N");

        public MsgHeadersTests()
            => UnitUnderTest = new MsgHeaders();

        [Fact]
        public void Can_manage_key_values()
        {
            var kv1 = (Key: NewValidKey(), Values: new[] {NewValidValue(), NewValidValue()});
            var kv2 = (Key: NewValidKey(), Values: new[] {NewValidValue(), NewValidValue()});
            var kv3 = (Key: NewValidKey(), Values: new[] {NewValidValue(), NewValidValue()});

            UnitUnderTest.Add(kv1.Key, kv1.Values[0]);
            UnitUnderTest.TryGetValue(kv1.Key, out var actualValues1).Should().BeTrue();
            UnitUnderTest.Count.Should().Be(1);
            actualValues1.Should().BeEquivalentTo(kv1.Values.Take(1));

            UnitUnderTest.Add(kv2.Key, kv2.Values[0]);
            UnitUnderTest.TryGetValue(kv2.Key, out var actualValues2).Should().BeTrue();
            UnitUnderTest.Count.Should().Be(2);
            actualValues2.Should().BeEquivalentTo(kv2.Values.Take(1));

            UnitUnderTest.Add(kv1.Key, kv1.Values[1]);
            UnitUnderTest.TryGetValue(kv1.Key, out actualValues1).Should().BeTrue();
            actualValues1.Should().BeEquivalentTo(kv1.Values);

            UnitUnderTest.Add(kv2.Key, kv2.Values[1]);
            UnitUnderTest.TryGetValue(kv2.Key, out actualValues2).Should().BeTrue();
            actualValues2.Should().BeEquivalentTo(kv2.Values);

            UnitUnderTest.Add(kv3.Key, NewValidValue());
            UnitUnderTest.Set(kv3.Key, kv3.Values);
            UnitUnderTest.TryGetValue(kv3.Key, out var actualValues3).Should().BeTrue();
            UnitUnderTest.Count.Should().Be(3);
            actualValues3.Should().BeEquivalentTo(kv3.Values);

            UnitUnderTest.Keys
                .OrderBy(k => k)
                .Should()
                .BeEquivalentTo(new[] {kv1.Key, kv2.Key, kv3.Key});

            UnitUnderTest.Values
                .SelectMany(vs => vs.ToArray())
                .OrderBy(v => v)
                .Should().BeEquivalentTo(kv1.Values.Union(kv2.Values).Union(kv3.Values).OrderBy(v => v));
        }

        [Theory]
        [InlineData("")]
        public void Accepts_missing_value_defined_by(string value)
        {
            var key = NewValidKey();

            UnitUnderTest.Add(key, value);

            UnitUnderTest.TryGetValue(key, out var actualValues).Should().BeTrue();
            actualValues.Should().Contain(value);
        }

        [Theory]
        [InlineData("ABCDEFGHIGKLMNOPQRSTUVWXYZ")]
        [InlineData("abcdefghigklmnopqrstuvwxyz")]
        [InlineData("0123456789")]
        [InlineData("@{}-/+\\.!#$[]()_*^~")]
        public void Can_handles_keys_with(string key)
        {
            UnitUnderTest.Add(key, string.Empty);
            UnitUnderTest.Set(key, new[] {string.Empty});
        }

        [Theory]
        [InlineData("ABCDEFGHIGKLMNOPQRSTUVWXYZ")]
        [InlineData("abcdefghigklmnopqrstuvwxyz")]
        [InlineData("0123456789")]
        [InlineData("@{}-/+.!#?$[]()_*^~? ")]
        public void Can_handles_values_with(string value)
        {
            UnitUnderTest.Add(NewValidKey(), value);
            UnitUnderTest.Set(NewValidKey(), new[] {value});
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("?")]
        [InlineData("'")]
        [InlineData("\"")]
        public void Key_can_not_contain(string key)
        {
            UnitUnderTest
                .Invoking(a => a.Add(key, string.Empty))
                .Should().Throw<ArgumentException>();

            UnitUnderTest
                .Invoking(a => a.Set(key, new[] {string.Empty}))
                .Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("'")]
        [InlineData("\"")]
        public void Value_can_not_contain(string key)
        {
            UnitUnderTest
                .Invoking(a => a.Add(key, string.Empty))
                .Should().Throw<ArgumentException>();

            UnitUnderTest
                .Invoking(a => a.Set(key, new[] {string.Empty}))
                .Should().Throw<ArgumentException>();
        }
    }
}
