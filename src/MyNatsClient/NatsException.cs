using System;

namespace NatsFun
{
    public class NatsException : Exception
    {
        public NatsException(string message) : base(message) { }

        internal static NatsException NoConnectionCouldBeMade()
            => new NatsException("No connection could be established against any of the specified hosts.");

        internal static NatsException ExceededMaxPayload(long maxPayload, long bufferLength)
            => new NatsException($"Server indicated max payload of {maxPayload} bytes. Current dispatch is {bufferLength} bytes.");
    }
}