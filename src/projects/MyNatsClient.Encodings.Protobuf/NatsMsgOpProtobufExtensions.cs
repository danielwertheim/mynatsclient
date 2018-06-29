using System;
using MyNatsClient.Ops;

namespace MyNatsClient.Encodings.Protobuf
{
    public static class NatsMsgOpProtobufExtensions
    {
        public static T FromProtobuf<T>(this MsgOp msgOp) where T : class
            => FromProtobuf(msgOp, typeof(T)) as T;

        public static object FromProtobuf(this MsgOp msgOp, Type objectType)
        {
            return ProtobufEncoding.Default.Decode(msgOp.Payload, objectType);
        }
    }
}