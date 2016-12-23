using System;

namespace MyNatsClient.Internals
{
    internal static class Disposable
    {
        internal static IDisposable Empty { get; }

        static Disposable()
        {
            Empty = new Impl();
        }

        private class Impl : IDisposable
        {
            public void Dispose() { }
        }
    }
}