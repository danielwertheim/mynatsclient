namespace MyNatsClient.Internals.Commands
{
    internal static class PongCmd
    {
        private static readonly byte[] Bytes = NatsEncoder.Encoding.GetBytes($"PONG{NatsEncoder.Crlf}");

        internal static byte[] Generate() => Bytes;
    }
}