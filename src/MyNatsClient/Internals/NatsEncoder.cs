using System.Text;

namespace MyNatsClient.Internals
{
    internal static class NatsEncoder
    {
        internal const string Crlf = "\r\n";
        internal static readonly Encoding Encoding = Encoding.UTF8;
        internal static byte[] GetBytes(string data) => Encoding.GetBytes(data);
        internal static string GetString(byte[] data) => Encoding.GetString(data);
    }
}