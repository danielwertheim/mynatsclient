namespace MyNatsClient.Internals.Commands
{
    internal static class SubCmd
    {
        internal static byte[] Generate(string subject, string subscriptionId, string queueGroup = null)
        {
            var s = queueGroup != null ? " " : string.Empty;

            return NatsEncoder.Encoding.GetBytes($"SUB {subject}{s}{queueGroup} {subscriptionId}{NatsEncoder.Crlf}");
        }
    }
}