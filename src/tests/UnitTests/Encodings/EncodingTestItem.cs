using System;
using ProtoBuf;

namespace UnitTests.Encodings
{
    [ProtoContract]
    public class EncodingTestItem
    {
        public static EncodingTestItem Create() => new EncodingTestItem
        {
            SomeInt = 42,
            SomeNullableInt = 42,
            SomeNullableIntBeingNull = null,

            SomeDecimal = 3.14M,
            SomeNullableDecimal = 3.14M,
            SomeNullableDecimalBeingNull = null,

            SomeDateTime = new DateTime(2018, 6, 29, 18, 34, 34),
            SomeNullableDateTime = new DateTime(2018, 6, 29, 18, 34, 34),
            SomeNullableDateTimeBeingNull = null,

            SomeString = $"This is a string with a random GUID being '{Guid.NewGuid():N}",
            SomeStringBeingNull = null
        };

        [ProtoMember(1)]
        public int SomeInt { get; set; }
        [ProtoMember(2)]
        public int? SomeNullableInt { get; set; }
        [ProtoMember(3)]
        public int? SomeNullableIntBeingNull { get; set; }

        [ProtoMember(4)]
        public decimal SomeDecimal { get; set; }
        [ProtoMember(5)]
        public decimal? SomeNullableDecimal { get; set; }
        [ProtoMember(6)]
        public decimal? SomeNullableDecimalBeingNull { get; set; }

        [ProtoMember(7)]
        public DateTime SomeDateTime { get; set; }
        [ProtoMember(8)]
        public DateTime? SomeNullableDateTime { get; set; }
        [ProtoMember(9)]
        public DateTime? SomeNullableDateTimeBeingNull { get; set; }

        [ProtoMember(10)]
        public string SomeString { get; set; }
        [ProtoMember(11)]
        public string SomeStringBeingNull { get; set; }
    }
}