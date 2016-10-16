using System;

namespace MyNatsClient.Internals
{
    internal static class Swallow
    {
        internal static void Everything(Action a)
        {
            try
            {
                a();
            }
            catch
            {
                // ignored
            }
        }
    }
}