using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MyNatsClient.Internals.Extensions
{
    internal static class TaskExtensions
    {
        internal static ConfiguredTaskAwaitable ForAwait(this Task t)
        {
            return t.ConfigureAwait(false);
        }

        internal static ConfiguredTaskAwaitable<T> ForAwait<T>(this Task<T> t)
        {
            return t.ConfigureAwait(false);
        }
    }
}