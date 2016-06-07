using System;

namespace MyNatsClient.Internals.Commands
{
    internal static class PubCmd
    {
        internal static byte[] Generate(string subject, string body, string replyTo = null)
            => Generate(subject, NatsEncoder.GetBytes(body), replyTo);

        internal static byte[] Generate(string subject, byte[] body, string replyTo = null)
        {
            var preBody = GeneratePreBody(subject, body.Length, replyTo);
            var crlfLen = NatsEncoder.CrlfBytes.Length;
            var buff = new byte[
                preBody.Length +
                crlfLen +
                body.Length +
                crlfLen];
            Array.Copy(preBody, buff, preBody.Length);
            Array.Copy(NatsEncoder.CrlfBytes, 0, buff, preBody.Length, crlfLen);
            Array.Copy(body, 0, buff, preBody.Length + crlfLen, body.Length);
            Array.Copy(NatsEncoder.CrlfBytes, 0, buff, preBody.Length + crlfLen + body.Length, crlfLen);

            return buff;
        }

        private static byte[] GeneratePreBody(string subject, int bodyLength, string replyTo = null)
        {
            var s = replyTo != null ? " " : string.Empty;

            return NatsEncoder.GetBytes($"PUB {subject}{s}{replyTo} {bodyLength}");
        }
    }
}