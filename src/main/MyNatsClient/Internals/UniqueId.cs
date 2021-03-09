using System;

namespace MyNatsClient.Internals
{
    internal static class UniqueId
    {
        private static readonly char[] Chars =
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
            'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
            'U', 'V'
        };

        internal static string Generate()
            => string.Create(13, DateTime.UtcNow.Ticks, (buffer, val) =>
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
