using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using MyNatsClient.Ops;

namespace IntegrationTests
{
    public class Sync : IDisposable
    {
        private static readonly TimeSpan MaxWaitTime = TimeSpan.FromMilliseconds(10000);

        private readonly int _maxNumOfRequests;
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentQueue<MsgOp> _interceptedMsgOps;

        public IEnumerable<MsgOp> Intercepted => _interceptedMsgOps;
        public int InterceptedCount => _interceptedMsgOps.Count;

        private Sync(int maxNumOfRequests)
        {
            _maxNumOfRequests = maxNumOfRequests;
            _semaphore = new SemaphoreSlim(0, maxNumOfRequests);
            _interceptedMsgOps = new ConcurrentQueue<MsgOp>();
        }

        public static Sync Max(int numOfRequests) => new Sync(numOfRequests);

        public static Sync MaxOne() => new Sync(1);

        public static Sync MaxTwo() => new Sync(2);

        public static Sync MaxThree() => new Sync(3);

        public void Release(MsgOp msgOp = null)
        {
            if(msgOp != null)
                _interceptedMsgOps.Enqueue(msgOp);

            _semaphore.Release(1);
        }

        private void Wait(int aquireCount, TimeSpan? ts = null)
        {
            ts ??= MaxWaitTime;

            if (System.Diagnostics.Debugger.IsAttached) 
                ts = TimeSpan.FromMilliseconds(-1);

            var aquiredCount = 0;
            using (var cts = new CancellationTokenSource(ts.Value))
            {
                for (var c = 0; c < aquireCount; c++)
                {
                    _semaphore.Wait(cts.Token);
                    aquiredCount++;
                }
            }

            if(aquiredCount != aquireCount)
                throw new Exception($"Aquired {aquiredCount} but should have aquired {aquireCount}.");
        }

        public void WaitForAny() => Wait(1);

        public void WaitForAll() => Wait(_maxNumOfRequests);

        public void Dispose() => _semaphore.Dispose();
    }
}