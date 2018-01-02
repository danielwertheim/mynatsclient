using System;

namespace MyNatsClient.Events
{
    public class ClientWorkerFailed : IClientEvent
    {
        public INatsClient Client { get; }
        public Exception Exception { get; }

        public ClientWorkerFailed(INatsClient client, Exception exception)
        {
            Client = client;
            Exception = exception;
        }
    }
}