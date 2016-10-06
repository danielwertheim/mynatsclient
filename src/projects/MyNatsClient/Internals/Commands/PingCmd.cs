namespace MyNatsClient.Internals.Commands
{
    internal static class PingCmd
    {
        private static readonly byte[] Bytes = NatsEncoder.Encoding.GetBytes($"PING{NatsEncoder.Crlf}");

        internal static byte[] Generate() => Bytes;
    }
}