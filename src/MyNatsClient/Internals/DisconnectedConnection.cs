using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyNatsClient.Internals
{
    /// <summary>
    /// Represents the state where MyNatsClient is disconnected, either through
    /// server failure or client action.
    /// </summary>
    internal sealed class DisconnectedConnection : INatsConnection
    {
        public static readonly INatsConnection Instance = new DisconnectedConnection();

        public bool IsConnected => false;

        public bool CanRead => false;

        public INatsServerInfo ServerInfo => throw NotConnected();
        public IEnumerable<IOp> ReadOp() => throw NotConnected();
        public void WithWriteLock(Action<INatsStreamWriter> a) => throw NotConnected();
        public void WithWriteLock<TArg>(Action<INatsStreamWriter, TArg> a, TArg arg) => throw NotConnected();
        public Task WithWriteLockAsync(Func<INatsStreamWriter, Task> a) => throw NotConnected();
        public Task WithWriteLockAsync<TArg>(Func<INatsStreamWriter, TArg, Task> a, TArg arg) => throw NotConnected();

        public void Dispose() { }

        private static InvalidOperationException NotConnected() => new InvalidOperationException("Connection has been disconnected.");
    }
}
