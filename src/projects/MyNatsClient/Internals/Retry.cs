using System;
using System.Threading.Tasks;

namespace MyNatsClient.Internals
{
    internal static class Retry
    {
        internal static T This<T>(Func<T> f, int maxCycleDelayMs, int maxDurationMs) where T : class
        {
            T r;
            var started = DateTime.Now;
            var n = 0;

            while(true)
            {
                r = f();
                if (r != null)
                    break;

                var delay = (n + n)*10;
                delay = delay > maxCycleDelayMs ? maxCycleDelayMs : delay;

                var duration = DateTime.Now.Subtract(started);
                if(duration.TotalMilliseconds + delay >= maxDurationMs)
                    break;

                Task.Delay(delay).Wait();

                n++;
            }

            return r;
        }
    }
}