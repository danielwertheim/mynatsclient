using System;

namespace MyNatsClient.Internals
{
    internal static class UniqueId
    {
        internal static string Generate() => Guid.NewGuid().ToString("N");
    }
}
