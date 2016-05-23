using System;
using System.Collections.Generic;

namespace MyNatsClient.Internals.Extensions
{
    internal static class ArrayExtensions
    {
        private static readonly Random Rnd = new Random();

        internal static T[] GetRandomized<T>(this T[] src)
        {
            var result = new T[src.Length];
            var range = new List<T>(src);

            for (var i = 0; i < src.Length; i++)
            {
                var rndIndex = Rnd.Next(0, range.Count - 1);
                result[i] = range[rndIndex];
                range.RemoveAt(rndIndex);
            }

            return result;
        }
    }
}