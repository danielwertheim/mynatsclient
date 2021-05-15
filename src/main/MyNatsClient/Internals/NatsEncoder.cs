using System;
using System.Text;

namespace MyNatsClient.Internals
{
    internal static class NatsEncoder
    {
        private static readonly Encoding Encoding = Encoding.UTF8;

        internal const string Crlf = "\r\n";
        internal static readonly byte[] CrlfBytes = { (byte)'\r', (byte)'\n' };
        internal static readonly int CrlfBytesLen = CrlfBytes.Length;
        internal static readonly byte SpaceByte = (byte)' ';

        internal static ReadOnlyMemory<byte> GetBytes(ReadOnlySpan<char> src)
            => Encoding.GetBytes(src.ToArray());

        internal static string GetString(ReadOnlySpan<byte> src)
            => Encoding.GetString(src);

        internal static string GetSingleByteCharString(ReadOnlySpan<byte> src)
            => string.Create(src.Length, src.ToArray(), (trg, v) =>
            {
                for (var i = 0; i < v.Length; i++)
                    trg[i] = (char)v[i];
            });

        internal static int WriteCrlf(Span<byte> trg, int trgOffset)
        {
            trg[trgOffset++] = CrlfBytes[0];
            trg[trgOffset++] = CrlfBytes[1];

            return trgOffset;
        }

        internal static int WriteSingleByteChars(Span<byte> trg, int trgOffset, ReadOnlySpan<char> src)
        {
            for (var i = 0; i < src.Length; i++)
                trg[trgOffset++] = (byte)src[i];

            return trgOffset;
        }
    }
}
