using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyNatsClient
{
    public interface INatsConnection : IDisposable
    {
        INatsServerInfo ServerInfo { get; }
        bool IsConnected { get; }
        bool CanRead { get; }

        IEnumerable<IOp> ReadOp();
        void WithWriteLock(Action<INatsStreamWriter> a);
        Task WithWriteLockAsync(Func<INatsStreamWriter, Task> a);
    }
}