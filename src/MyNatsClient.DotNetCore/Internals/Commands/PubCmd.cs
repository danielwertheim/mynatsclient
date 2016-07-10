using System;

namespace MyNatsClient.Internals.Commands
{
    internal static class PubCmd
    {
        private static readonly byte[] Cmd = { (byte)'P', (byte)'U', (byte)'B' };

        internal static byte[] Generate(string subject, string body, string replyTo = null)
            => Generate(subject, NatsEncoder.GetBytes(body), replyTo);

        internal static byte[] Generate(string subject, byte[] body, string replyTo = null)
        {
            var bodyLenString = body.Length.ToString();
            var preBodyLen = 3 + 1 + subject.Length + (replyTo?.Length + 1 ?? 0) + 1 + bodyLenString.Length;
            var buff = new byte[
                preBodyLen +
                NatsEncoder.CrlfBytesLen +
                body.Length +
                NatsEncoder.CrlfBytesLen];

            FillPreBody(buff, subject, bodyLenString, replyTo);
            Buffer.BlockCopy(NatsEncoder.CrlfBytes, 0, buff, preBodyLen, NatsEncoder.CrlfBytesLen);
            Buffer.BlockCopy(body, 0, buff, preBodyLen + NatsEncoder.CrlfBytesLen, body.Length);
            Buffer.BlockCopy(NatsEncoder.CrlfBytes, 0, buff, preBodyLen + NatsEncoder.CrlfBytesLen + body.Length, NatsEncoder.CrlfBytesLen);

            return buff;
        }

        internal static IPayload Generate(string subject, IPayload body, string replyTo = null)
        {
            var bodySizeString = body.Size.ToString();
            var preBodyLen = 3 + 1 + subject.Length + (replyTo?.Length + 1 ?? 0) + 1 + bodySizeString.Length;
            var preBody = new byte[preBodyLen];
            FillPreBody(preBody, subject, bodySizeString, replyTo);

            var pubCmd = new PayloadBuilder();
            pubCmd.Append(preBody);
            pubCmd.Append(NatsEncoder.CrlfBytes);
            pubCmd.Append(body);
            pubCmd.Append(NatsEncoder.CrlfBytes);

            return pubCmd.ToPayload();
        }

        private static void FillPreBody(byte[] buff, string subject, string bodyLenString, string replyTo = null)
        {
            buff[0] = Cmd[0];
            buff[1] = Cmd[1];
            buff[2] = Cmd[2];
            buff[3] = NatsEncoder.SpaceByte;

            var curr = 3;
            curr = NatsEncoder.WriteSingleByteUtf8String(buff, subject, curr);

            if (replyTo != null)
            {
                buff[++curr] = NatsEncoder.SpaceByte;
                curr = NatsEncoder.WriteSingleByteUtf8String(buff, replyTo, curr);
            }
            buff[++curr] = NatsEncoder.SpaceByte;
            NatsEncoder.WriteSingleByteUtf8String(buff, bodyLenString, curr);
        }
    }
}