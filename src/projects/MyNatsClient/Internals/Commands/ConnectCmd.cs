using System.Text;

namespace MyNatsClient.Internals.Commands
{
    internal static class ConnectCmd
    {
        internal static byte[] Generate(bool verbose, Credentials credentials)
        {
            var opString = GenerateConnectionOpString(verbose, credentials);

            return NatsEncoder.Encoding.GetBytes(opString);
        }

        private static string GenerateConnectionOpString(bool verbose, Credentials credentials)
        {
            var sb = new StringBuilder();
            sb.Append("CONNECT {\"name\":\"mynatsclient\",\"lang\":\"csharp\",\"verbose\":");
            sb.Append(verbose.ToString().ToLower());

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