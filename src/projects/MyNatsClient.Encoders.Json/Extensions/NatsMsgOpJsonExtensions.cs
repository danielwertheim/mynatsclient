using System;
using MyNatsClient.Ops;

namespace MyNatsClient.Encodings.Json.Extensions
{
    public static class NatsMsgOpJsonExtensions
    {
        public static object FromJson(this MsgOp msgOp, Type objectType)
        {
            return JsonEncoding.Default.Decode(msgOp.Payload, objectType);
        }
    }
}