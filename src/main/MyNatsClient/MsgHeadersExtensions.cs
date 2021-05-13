using System;
using System.Text;
using MyNatsClient.Internals;

namespace MyNatsClient
{
    internal static class MsgHeadersExtensions
    {
        internal static ReadOnlySpan<char> AsSpan(this IMsgHeaders headers)
        {
            var sb = new StringBuilder();
            sb.Append(headers.Protocol);
            sb.Append(NatsEncoder.Crlf);

            foreach (var (key, values) in headers)
            {
                foreach (var value in values)
                {
                    sb.Append(key);
                    sb.Append(':');
                    sb.Append(value ?? string.Empty);
                    sb.Append(NatsEncoder.Crlf);
                }
            }

            sb.Append(NatsEncoder.Crlf);

            return sb.ToString().AsSpan();
        }

        internal static ReadOnlyMemory<char> AsMemory(this IMsgHeaders headers) => AsSpan(headers).ToArray();
    }
}
