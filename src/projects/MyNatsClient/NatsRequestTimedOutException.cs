namespace MyNatsClient
{
    public class NatsRequestTimedOutException : NatsException
    {
        public NatsRequestTimedOutException()
            : base(NatsExceptionCodes.RequestTimedOut, "Request timed out.") {}
    }
}