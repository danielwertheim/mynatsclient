namespace MyNatsClient.Internals.Commands
{
    internal static class UnsubCmd
    {
        internal static byte[] Generate(string subscriptionId, int? maxMessages = null)
        {
            var s = maxMessages.HasValue ? " " : string.Empty;

            return NatsEncoder.Encoding.GetBytes($"UNSUB {subscriptionId}{s}{maxMessages}{NatsEncoder.Crlf}");
        }
    }
}