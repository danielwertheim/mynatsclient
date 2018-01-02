namespace MyNatsClient
{
    public static class NatsExceptionCodes
    {
        internal const string Unknown = "Unknown";
        public const string CouldNotCreateSubscription = "CouldNotCreateSubscription";
        public const string MissingCredentials = "MissingCredentials";
        public const string FailedToConnectToHost = "FailedToConnectToHost";
        public const string CouldNotEstablishAnyConnection = "CouldNotEstablishAnyConnection";
        public const string ExceededMaxPayload = "ExceededMaxPayload";
        public const string NoDataReceivedFromServer = "NoDataReceivedFromServer";
        public const string RequestTimedOut = "RequestTimedOut";
        public const string ConnectionFoundIdling = "ConnectionFoundIdling";
        public const string ClientReceivedErrOp = "ClientReceivedErrOp";
    }
}