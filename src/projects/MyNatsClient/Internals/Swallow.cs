using System;

namespace MyNatsClient.Internals
{
    internal static class Swallow
    {
        internal static void Everything(params Action[] actions)
        {
            if (actions == null)
                return;

            for (var i = 0; i < actions.Length; i++)
                try
                {
                    actions[i]();
                }
                catch
                {
                    // ignored
                }
        }
    }
}