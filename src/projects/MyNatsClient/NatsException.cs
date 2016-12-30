using System;
using EnsureThat;

namespace MyNatsClient
{
    public class NatsException : AggregateException
    {
        public string ExceptionCode { get; }

        protected NatsException(string exceptionCode, string message, params Exception[] innerExceptions)
            : base(message, innerExceptions)
        {
            EnsureArg.IsNotNullOrWhiteSpace(exceptionCode, nameof(exceptionCode));

            ExceptionCode = exceptionCode;
        }

        internal static NatsException MissingCredentials(string host)
            => new NatsException(NatsExceptionCodes.MissingCredentials, $"Error while connecting to {host}. Server requires credentials to be passed. None was specified.");

        internal static NatsException NoConnectionCouldBeMade(params Exception[] exceptions)
            => new NatsException(NatsExceptionCodes.NoConnectionCouldBeMade, "No connection could be established against any of the specified servers.", exceptions);

        internal static NatsException ExceededMaxPayload(long maxPayload, long bufferLength)
            => new NatsException(NatsExceptionCodes.ExceededMaxPayload, $"Server indicated max payload of {maxPayload} bytes. Current dispatch is {bufferLength} bytes.");

        internal static NatsException NoDataReceivedFromServer()
            => new NatsException(NatsExceptionCodes.NoDataReceivedFromServer, "Have not received any data from server lately.");

        internal static NatsException RequestTimedOut()
            => new NatsRequestTimedOutException();

        internal static NatsException CouldNotCreateSubscription(SubscriptionInfo subscriptionInfo)
            => new NatsException(NatsExceptionCodes.CouldNotCreateSubscription, $"Could not create subscription. Id='{subscriptionInfo.Id}'. Subject='{subscriptionInfo.Subject}' QueueGroup='{subscriptionInfo.QueueGroup}'.");

        internal static NatsException ConnectionFoundStale(string host, int port)
            => new NatsException(NatsExceptionCodes.ConnectionFoundStale, $"The Connection against server {host}:{port.ToString()} has not received any data in to long period.");
    }
}