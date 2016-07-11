using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace MyNatsClient.UnitTests
{
    public class PayloadBuilderTests : UnitTestsOf<PayloadBuilder>
    {
        private const byte SomeAsciiByte = (byte)'a';

        protected override void OnBeforeEachTest()
        {
            UnitUnderTest = new PayloadBuilder();
        }

        [Test]
        public void Append_Should_produce_one_block_When_appending_byte_by_byte_less_then_block_size()
        {
            var numOfBytesToAdd = PayloadBuilder.BlockSize - 1;
            var bytesToAdd = GetBytesToAdd(numOfBytesToAdd);

            foreach (var b in bytesToAdd)
                UnitUnderTest.Append(b);

            var payload = UnitUnderTest.ToPayload();
            payload.BlockCount.Should().Be(1);
            payload.Size.Should().Be(numOfBytesToAdd);
            payload.Should().BeEquivalentTo(bytesToAdd);
        }

        [Test]
        public void Append_Should_produce_one_block_When_appending_byte_by_byte_same_as_block_size()
        {
            var numOfBytesToAdd = PayloadBuilder.BlockSize;
            var bytesToAdd = GetBytesToAdd(numOfBytesToAdd);

            foreach (var b in bytesToAdd)
                UnitUnderTest.Append(b);

            var payload = UnitUnderTest.ToPayload();
            payload.BlockCount.Should().Be(1);
            payload.Size.Should().Be(numOfBytesToAdd);
            payload.Should().BeEquivalentTo(bytesToAdd);
        }

        [Test]
        public void Append_Should_produce_two_blocks_When_appending_byte_by_byte_one_more_then_block_size()
        {
            var numOfBytesToAdd = PayloadBuilder.BlockSize + 1;
            var bytesToAdd = GetBytesToAdd(numOfBytesToAdd);

            foreach (var b in bytesToAdd)
                UnitUnderTest.Append(b);

            var payload = UnitUnderTest.ToPayload();
            payload.BlockCount.Should().Be(2);
            payload.Size.Should().Be(numOfBytesToAdd);
            payload.Should().BeEquivalentTo(bytesToAdd);
        }

        [Test]
        public void Append_Should_produce_one_block_When_appending_bytes_by_byte_less_then_block_size()
        {
            var numOfBytesToAdd = PayloadBuilder.BlockSize - 1;
            var bytesToAdd = GetBytesToAdd(numOfBytesToAdd);

            UnitUnderTest.Append(bytesToAdd);

            var payload = UnitUnderTest.ToPayload();
            payload.BlockCount.Should().Be(1);
            payload.Size.Should().Be(numOfBytesToAdd);
            payload.Should().BeEquivalentTo(bytesToAdd);
        }

        [Test]
        public void Append_Should_produce_one_block_When_appending_byte_by_bytes_same_as_block_size()
        {
            var numOfBytesToAdd = PayloadBuilder.BlockSize;
            var bytesToAdd = GetBytesToAdd(numOfBytesToAdd);

            UnitUnderTest.Append(bytesToAdd);

            var payload = UnitUnderTest.ToPayload();
            payload.BlockCount.Should().Be(1);
            payload.Size.Should().Be(numOfBytesToAdd);
            payload.Should().BeEquivalentTo(bytesToAdd);
        }

        [Test]
        public void Append_Should_produce_two_blocks_When_appending_byte_by_bytes_one_more_then_block_size()
        {
            var numOfBytesToAdd = PayloadBuilder.BlockSize + 1;
            var bytesToAdd = GetBytesToAdd(numOfBytesToAdd);

            UnitUnderTest.Append(bytesToAdd);

            var payload = UnitUnderTest.ToPayload();
            payload.BlockCount.Should().Be(2);
            payload.Size.Should().Be(numOfBytesToAdd);
            payload.Should().BeEquivalentTo(bytesToAdd);
        }

        [Test]
        public void Append_Should_produce_one_block_When_appending_payload_with_bytes_less_then_block_size()
        {
            var numOfBytesToAdd = PayloadBuilder.BlockSize - 1;
            var bytesToAdd = GetBytesToAdd(numOfBytesToAdd);
            var payloadFake = GetPayloadFake(bytesToAdd);

            UnitUnderTest.Append(payloadFake);

            var payload = UnitUnderTest.ToPayload();
            payload.BlockCount.Should().Be(1);
            payload.Size.Should().Be(numOfBytesToAdd);
            payload.Should().BeEquivalentTo(bytesToAdd);
        }

        [Test]
        public void Append_Should_produce_one_block_When_appending_payload_with_bytes_same_as_block_size()
        {
            var numOfBytesToAdd = PayloadBuilder.BlockSize;
            var bytesToAdd = GetBytesToAdd(numOfBytesToAdd);
            var payloadFake = GetPayloadFake(bytesToAdd);

            UnitUnderTest.Append(payloadFake);

            var payload = UnitUnderTest.ToPayload();
            payload.BlockCount.Should().Be(1);
            payload.Size.Should().Be(numOfBytesToAdd);
            payload.Should().BeEquivalentTo(bytesToAdd);
        }

        [Test]
        public void Append_Should_produce_two_blocks_When_appending_payload_with_bytes_being_one_more_then_block_size()
        {
            var bytesToAdd1 = GetBytesToAdd(PayloadBuilder.BlockSize);
            var bytesToAdd2 = GetBytesToAdd(1);
            var payloadFake = GetPayloadFake(bytesToAdd1, bytesToAdd2);

            UnitUnderTest.Append(payloadFake);

            var payload = UnitUnderTest.ToPayload();
            payload.BlockCount.Should().Be(2);
            payload.Size.Should().Be(bytesToAdd1.Length + bytesToAdd2.Length);
            payload.Should().BeEquivalentTo(new List<byte[]> { bytesToAdd1, bytesToAdd2 }.SelectMany(i => i));
        }

        private static IPayload GetPayloadFake(params byte[][] blocks)
        {
            var b = new List<byte[]>();
            b.AddRange(blocks);
            var byteSize = b.Sum(i => i.Length);

            var payloadFake = new Mock<IPayload>();
            payloadFake
                .Setup(f => f.Blocks)
                .Returns(b);
            payloadFake
                .Setup(f => f.BlockCount)
                .Returns(1);
            payloadFake
                .Setup(f => f.Size)
                .Returns(byteSize);
            payloadFake
                .Setup(f => f.GetEnumerator())
                .Returns(b.SelectMany(i => i).GetEnumerator);

            return payloadFake.Object;
        }

        private static byte[] GetBytesToAdd(int numOfBytesToAdd)
        {
            var r = new byte[numOfBytesToAdd];
            byte c = 0;
            for (var i = 0; i < numOfBytesToAdd; i++)
            {
                r[i] = (byte)(SomeAsciiByte + c);
                c += 1;
                c = c % 10 == 0 ? (byte)0 : c;
            }
            return r;
        }
    }
}