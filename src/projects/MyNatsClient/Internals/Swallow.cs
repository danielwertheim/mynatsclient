using System;

namespace MyNatsClient.Internals
{
    internal static class Swallow
    {
        internal static void Everything(params Action[] actions)
        {
            foreach (var action in actions)
            {
                try
                {
                    action();
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}