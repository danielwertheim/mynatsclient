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

        internal static NatsException MissingCredentials(Host host)
            => new NatsException(NatsExceptionCodes.MissingCredentials, $"Error while connecting to {host}. Host requires credentials to be passed. None was specified. Pass for specific host or for all hosts.");

        internal static NatsException FailedToConnectToHost(Host host, string message)
            => new NatsException(NatsExceptionCodes.FailedToConnectToHost, $"Error while connecting to {host}. {message}");

        internal static NatsException CouldNotEstablishAnyConnection(params Exception[] exceptions)
            => new NatsException(NatsExceptionCodes.CouldNotEstablishAnyConnection, "No connection could be established against any of the specified hosts (servers).", exceptions);

        internal static NatsException ExceededMaxPayload(long maxPayload, long bufferLength)
            => new NatsException(NatsExceptionCodes.ExceededMaxPayload, $"Server indicated max payload of {maxPayload} bytes. Current dispatch is {bufferLength} bytes.");

        internal static NatsException NoDataReceivedFromServer()
            => new NatsException(NatsExceptionCodes.NoDataReceivedFromServer, "Have not received any data from server lately.");

        internal static NatsException RequestTimedOut()
            => new NatsRequestTimedOutException();

        internal static NatsException CouldNotCreateSubscription(SubscriptionInfo subscriptionInfo)
            => new NatsException(NatsExceptionCodes.CouldNotCreateSubscription, $"Could not create subscription. Id='{subscriptionInfo.Id}'. Subject='{subscriptionInfo.Subject}' QueueGroup='{subscriptionInfo.QueueGroup}'.");

        internal static NatsException ConnectionFoundIdling(string host, int port)
            => new NatsException(NatsExceptionCodes.ConnectionFoundIdling, $"The Connection against server {host}:{port.ToString()} has not received any data in a to long period.");
    }
}