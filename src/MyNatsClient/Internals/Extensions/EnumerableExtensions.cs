using System.Collections.Generic;

namespace MyNatsClient.Internals.Extensions
{
    internal static class EnumerableExtensions
    {
        internal static IEnumerable<T> CombineWith<T>(this IEnumerable<T> src1, IEnumerable<T> src2)
        {
            foreach (var i in src1)
                yield return i;

            foreach (var i in src2)
                yield return i;
        }
    }
}