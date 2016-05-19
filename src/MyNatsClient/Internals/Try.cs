using System;
using System.Collections.Generic;
using System.Linq;

namespace NatsFun.Internals
{
    internal static class Try
    {
        internal static void DisposeAll(IEnumerable<IDisposable> disposables)
        {
            var exs = new List<Exception>();

            foreach (var disposable in disposables)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    exs.Add(ex);
                }
            }

            if (exs.Any())
                throw new AggregateException(exs);
        }

        internal static void All(params Action[] actions)
        {
            var exs = new List<Exception>();

            foreach (var action in actions)
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    exs.Add(ex);
                }
            }

            if (exs.Any())
                throw new AggregateException(exs);
        }
    }
}