using System;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public class NatsException : Exception
    {
        public string ExceptionCode { get; private set; }

        protected NatsException(string exceptionCode, string message)
            : base(message)
        {
            ExceptionCode = exceptionCode ?? NatsExceptionCodes.Unknown;
        }

        internal static NatsException MissingCredentials(Host host)
            => new NatsException(NatsExceptionCodes.MissingCredentials, $"Error while connecting to {host}. Host requires credentials to be passed. None was specified. Pass for specific host or for all hosts.");

        internal static NatsException FailedToConnectToHost(Host host, string message)
            => new NatsException(NatsExceptionCodes.FailedToConnectToHost, $"Error while connecting to {host}. {message}");

        internal static NatsException CouldNotEstablishAnyConnection()
            => new NatsException(NatsExceptionCodes.CouldNotEstablishAnyConnection, "No connection could be established against any of the specified hosts (servers).");

        internal static NatsException ExceededMaxPayload(long maxPayload, long bufferLength)
            => new NatsException(NatsExceptionCodes.ExceededMaxPayload, $"Server indicated max payload of {maxPayload} bytes. Current dispatch is {bufferLength} bytes.");

        internal static NatsException CouldNotCreateSubscription(SubscriptionInfo subscriptionInfo)
            => new NatsException(NatsExceptionCodes.CouldNotCreateSubscription, $"Could not create subscription. Id='{subscriptionInfo.Id}'. Subject='{subscriptionInfo.Subject}' QueueGroup='{subscriptionInfo.QueueGroup}'.");

        internal static NatsException ConnectionFoundIdling(string host, int port)
            => new NatsException(NatsExceptionCodes.ConnectionFoundIdling, $"The Connection against server {host}:{port.ToString()} has not received any data in a to long period.");

        internal static NatsException ClientReceivedErrOp(ErrOp errOp)
            => new NatsException(NatsExceptionCodes.ClientReceivedErrOp, $"Client received ErrOp with message='{errOp.Message}'.");

        internal static NatsException OpParserError(string message)
            => new NatsException(NatsExceptionCodes.OpParserError, message);

        internal static NatsException OpParserUnsupportedOp(string opMarker)
            => new NatsException(NatsExceptionCodes.OpParserUnsupportedOp, $"Unsupported OP, don't know how to parse OP '{opMarker}'.");

        internal static NatsException OpParserOpParsingError(string op, byte expected, byte got)
            => new NatsException(NatsExceptionCodes.OpParserOpParsingError, $"Error while parsing {op}. Expected char code '{expected}' got '{got}'.");

        internal static NatsException OpParserOpParsingError(string op, string message)
            => new NatsException(NatsExceptionCodes.OpParserOpParsingError, $"Error while parsing {op}. {message}");

        internal static NatsException InitRequestError(string message)
        => new NatsException(NatsExceptionCodes.InitRequestError, message);
    }
}