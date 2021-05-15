namespace MyNatsClient
{
    public static class NatsExceptionCodes
    {
        internal const string Unknown = "Unknown";
        public const string CouldNotCreateSubscription = "CouldNotCreateSubscription";
        public const string MissingCredentials = "MissingCredentials";
        public const string MissingClientCertificates = "MissingClientCertificates";
        public const string FailedToConnectToHost = "FailedToConnectToHost";
        public const string CouldNotEstablishAnyConnection = "CouldNotEstablishAnyConnection";
        public const string ExceededMaxPayload = "ExceededMaxPayload";
        public const string ConnectionFoundIdling = "ConnectionFoundIdling";
        public const string ClientReceivedErrOp = "ClientReceivedErrOp";
        public const string ClientCouldNotConsumeStream = "ClientCouldNotConsumeStream";
        public const string OpParserError = "OpParser.Error";
        public const string OpParserOpParsingError = "OpParser.Error";
        public const string OpParserUnsupportedOp = "OpParser.UnsupportedOp";
        public const string InitRequestError = "Client.InitRequestError";
        public const string NotConnected = "NotConnected";
    }
}
