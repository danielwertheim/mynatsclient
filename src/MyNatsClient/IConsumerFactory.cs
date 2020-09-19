using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyNatsClient
{
    /// <summary>
    /// Responsible for returning a Task that continiously runs the consumer.
    /// </summary>
    public interface IConsumerFactory
    {
        Task Run(Action consumer, CancellationToken cancellationToken);
    }
}
