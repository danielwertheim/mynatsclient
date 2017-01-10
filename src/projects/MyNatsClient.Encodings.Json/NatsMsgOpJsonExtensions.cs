using System;
using MyNatsClient.Ops;

namespace MyNatsClient.Encodings.Json
{
    public static class NatsMsgOpJsonExtensions
    {
        public static T FromJson<T>(this MsgOp msgOp) where T : class
            => FromJson(msgOp, typeof(T)) as T;

        public static object FromJson(this MsgOp msgOp, Type objectType)
        {
            return JsonEncoding.Default.Decode(msgOp.Payload, objectType);
        }
    }
}