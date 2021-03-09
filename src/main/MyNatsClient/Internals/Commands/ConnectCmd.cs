using System;
using System.Text;

namespace MyNatsClient.Internals.Commands
{
    internal static class ConnectCmd
    {
        internal static ReadOnlySpan<byte> Generate(bool verbose, Credentials credentials, string name)
        {
            var opString = GenerateConnectionOpString(
                verbose,
                credentials ?? Credentials.Empty,
                name);

            return NatsEncoder.GetBytes(opString).Span;
        }

        private static string GenerateConnectionOpString(bool verbose, Credentials credentials, string name)
        {
            var sb = new StringBuilder();
            sb.Append("CONNECT {\"name\":\"");
            sb.Append(name);
            sb.Append("\",\"lang\":\"csharp\",\"protocol\":1,\"pedantic\":false,\"verbose\":");
            sb.Append(verbose ? "true" : "false");

            if (credentials != Credentials.Empty)
            {
                sb.Append(",\"user\":\"");
                sb.Append(credentials.User);
                sb.Append("\",\"pass\":\"");
                sb.Append(credentials.Pass);
                sb.Append("\"");
            }
            sb.Append("}");
            sb.Append(NatsEncoder.Crlf);

            return sb.ToString();
        }
    }
}
