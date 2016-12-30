namespace MyNatsClient
{
    public static class NatsExceptionCodes
    {
        public const string CouldNotCreateSubscription = "CouldNotCreateSubscription";
        public const string MissingCredentials = "MissingCredentials";
        public const string NoConnectionCouldBeMade = "NoConnectionCouldBeMade";
        public const string ExceededMaxPayload = "ExceededMaxPayload";
        public const string NoDataReceivedFromServer = "NoDataReceivedFromServer";
        public const string RequestTimedOut = "RequestTimedOut";
        public const string ConnectionFoundStale = "ConnectionFoundStale";
    }
}