using System.IO;

namespace MyNatsClient.Internals.Extensions
{
    internal static class StreamExtensions
    {
        internal static char ReadChar(this Stream s) => (char)s.ReadByte();
    }
}