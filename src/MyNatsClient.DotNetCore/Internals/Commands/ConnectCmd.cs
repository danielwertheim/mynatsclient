using System.Text;

namespace MyNatsClient.Internals.Commands
{
    internal static class ConnectCmd
    {
        internal static byte[] Generate(ConnectionInfo connectionInfo)
        {
            var opString = GenerateConnectionOpString(connectionInfo);

            return NatsEncoder.Encoding.GetBytes(opString);
        }

        private static string GenerateConnectionOpString(ConnectionInfo connectionInfo)
        {
            var sb = new StringBuilder();
            sb.Append("CONNECT {\"name\":\"mynatsclient\",\"lang\":\"csharp\",\"verbose\":");
            sb.Append(connectionInfo.Verbose.ToString().ToLower());

            if (connectionInfo.Credentials != Credentials.Empty)
            {
                sb.Append(",\"user\":\"");
                sb.Append(connectionInfo.Credentials.User);
                sb.Append("\",\"pass\":\"");
                sb.Append(connectionInfo.Credentials.Pass);
                sb.Append("\"");
            }
            sb.Append("}");
            sb.Append(NatsEncoder.Crlf);

            return sb.ToString();
        }
    }
}