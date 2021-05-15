using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyNatsClient
{
    public interface INatsConnection : IDisposable
    {
        INatsServerInfo ServerInfo { get; }
        bool IsConnected { get; }

        IEnumerable<IOp> ReadOps();
        void WithWriteLock(Action<INatsStreamWriter> a);
        void WithWriteLock<TArg>(Action<INatsStreamWriter, TArg> a, TArg arg);
        Task WithWriteLockAsync(Func<INatsStreamWriter, Task> a);
        Task WithWriteLockAsync<TArg>(Func<INatsStreamWriter, TArg, Task> a, TArg arg);
    }
}
