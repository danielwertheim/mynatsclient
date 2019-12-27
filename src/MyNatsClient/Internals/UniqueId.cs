using System;

namespace MyNatsClient.Internals
{
    internal static class UniqueId
    {
        private static readonly char[] Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUV".ToCharArray();

        internal static string Generate() => Generate(DateTime.UtcNow.Ticks);

        private static string Generate(long id) =>
            string.Create(13, id, (buffer, val) =>
            {
                var chars = Chars;

                buffer[12] = chars[val & 31];
                buffer[11] = chars[(val >> 5) & 31];
                buffer[10] = chars[(val >> 10) & 31];
                buffer[9] = chars[(val >> 15) & 31];
                buffer[8] = chars[(val >> 20) & 31];
                buffer[7] = chars[(val >> 25) & 31];
                buffer[6] = chars[(val >> 30) & 31];
                buffer[5] = chars[(val >> 35) & 31];
                buffer[4] = chars[(val >> 40) & 31];
                buffer[3] = chars[(val >> 45) & 31];
                buffer[2] = chars[(val >> 50) & 31];
                buffer[1] = chars[(val >> 55) & 31];
                buffer[0] = chars[(val >> 60) & 31];
            });
    }
}