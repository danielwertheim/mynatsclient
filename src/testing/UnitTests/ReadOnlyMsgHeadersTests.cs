using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MyNatsClient;
using Xunit;

namespace UnitTests
{
    public class ReadOnlyMsgHeadersTests : UnitTestsOf<ReadOnlyMsgHeaders>
    {
        private const string ValidProtocol = "NATS/1.0";

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("NATS_")]
        public void Creating_requires_protocol_to_start_with_NATS(string protocol)
        {
            Action creating = () => ReadOnlyMsgHeaders.Create(protocol, new Dictionary<string, IReadOnlyList<string>>());

            creating
                .Should()
                .ThrowExactly<ArgumentException>()
                .WithMessage("Protocol must start with 'NATS/'. (Parameter 'protocol')")
                .And.ParamName
                .Should().Be("protocol");
        }

        [Fact]
        public void Has_an_Empty_implementation()
        {
            UnitUnderTest = ReadOnlyMsgHeaders.Empty;

            UnitUnderTest.Should().BeEmpty();
            UnitUnderTest.Count.Should().Be(0);
            UnitUnderTest.Keys.Should().BeEmpty();
            UnitUnderTest.Values.Should().BeEmpty();
            UnitUnderTest.ContainsKey(Guid.NewGuid().ToString("N")).Should().BeFalse();
            UnitUnderTest.TryGetValue(Guid.NewGuid().ToString("N"), out var match).Should().BeFalse();
            match.Should().BeNull();
        }

        [Fact]
        public void Exposes_protocol()
        {
            UnitUnderTest = ReadOnlyMsgHeaders.Create(ValidProtocol, new Dictionary<string, IReadOnlyList<string>>());

            UnitUnderTest.Protocol.Should().Be(ValidProtocol);
        }

        [Fact]
        public void Works_as_a_read_only_dictionary()
        {
            var initialKvs = new Dictionary<string, IReadOnlyList<string>>
            {
                {"Header1", new[] {"Value1.1"}},
                {"Header2", new[] {"Value2.1", "Value2.2"}}
            };

            UnitUnderTest = ReadOnlyMsgHeaders.Create(ValidProtocol, initialKvs);

            UnitUnderTest.Count.Should().Be(2);
            UnitUnderTest.Keys.Should().BeEquivalentTo("Header1", "Header2");
            UnitUnderTest.Values.SelectMany(v => v).Should().BeEquivalentTo("Value1.1", "Value2.1", "Value2.2");
            UnitUnderTest["Header1"].Should().BeEquivalentTo("Value1.1");
            UnitUnderTest["Header2"].Should().BeEquivalentTo("Value2.1", "Value2.2");
            UnitUnderTest.ContainsKey("Header1").Should().BeTrue();
            UnitUnderTest.ContainsKey("MissingHeader").Should().BeFalse();
            UnitUnderTest.TryGetValue("Header1", out var matchingValues1).Should().BeTrue();
            matchingValues1.Should().BeEquivalentTo("Value1.1");
            UnitUnderTest.TryGetValue("Header2", out var matchingValues2).Should().BeTrue();
            matchingValues2.Should().BeEquivalentTo("Value2.1", "Value2.2");
            UnitUnderTest.TryGetValue("MissingHeader", out var missingValues).Should().BeFalse();
            missingValues.Should().BeNull();
        }
    }
}
