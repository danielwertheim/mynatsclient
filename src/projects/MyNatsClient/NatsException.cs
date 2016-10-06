using System;

namespace MyNatsClient
{
    public class NatsException : AggregateException
    {
        public NatsException(string message, params Exception[] innerExceptions) : base(message, innerExceptions) { }

        internal static NatsException NoConnectionCouldBeMade(params Exception[] exceptions)
            => new NatsException("No connection could be established against any of the specified servers.", exceptions);

        internal static NatsException ExceededMaxPayload(long maxPayload, long bufferLength)
            => new NatsException($"Server indicated max payload of {maxPayload} bytes. Current dispatch is {bufferLength} bytes.");

        public static NatsException NoDataReceivedFromServer()
            => new NatsException("Have not received any data from server lately.");
    }
}