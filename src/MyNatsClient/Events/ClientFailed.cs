using System;

namespace MyNatsClient.Events
{
    public class ClientFailed : IClientEvent
    {
        public INatsClient Client { get; }
        public Exception Exception { get; }

        public ClientFailed(INatsClient client, Exception exception)
        {
            Client = client;
            Exception = exception;
        }
    }
}