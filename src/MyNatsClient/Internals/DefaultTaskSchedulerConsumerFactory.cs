using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyNatsClient.Internals
{
    internal class DefaultTaskSchedulerConsumerFactory : IConsumerFactory
    {
        public Task Run(Action consumer, CancellationToken cancellationToken)
            => Task.Factory.StartNew(
                consumer,
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
    }
}
