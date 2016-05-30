using System.Text;

namespace MyNatsClient.Internals
{
    internal static class NatsEncoder
    {
        internal static readonly Encoding Encoding = Encoding.UTF8;
        internal const string Crlf = "\r\n";
        internal static readonly byte[] CrlfBytes = Encoding.GetBytes("\r\b");
        internal static byte[] GetBytes(string data) => Encoding.GetBytes(data);
        internal static string GetString(byte[] data) => Encoding.GetString(data);
    }
}