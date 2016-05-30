using System.Linq;
using MyNatsClient.Internals.Extensions;

namespace MyNatsClient.Internals.Commands
{
    internal static class PubCmd
    {
        internal static byte[] Generate(string subject, string body, string replyTo = null)
            => Generate(subject, NatsEncoder.GetBytes(body), replyTo);

        internal static byte[] Generate(string subject, byte[] body, string replyTo = null)
        {
            return GeneratePreBody(subject, body.Length, replyTo)
                .CombineWith(body)
                .CombineWith(GenerateAfterBody())
                .ToArray();
        }

        private static byte[] GeneratePreBody(string subject, int bodyLength, string replyTo = null)
        {
            var s = replyTo != null ? " " : string.Empty;

            return NatsEncoder.GetBytes($"PUB {subject}{s}{replyTo} {bodyLength}{NatsEncoder.Crlf}");
        }

        private static byte[] GenerateAfterBody() => NatsEncoder.GetBytes(NatsEncoder.Crlf);
    }
}