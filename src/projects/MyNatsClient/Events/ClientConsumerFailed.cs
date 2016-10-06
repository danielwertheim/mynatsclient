using System;

namespace MyNatsClient.Events
{
    public class ClientConsumerFailed : IClientEvent
    {
        public INatsClient Client { get; }
        public Exception Exception { get; }

        public ClientConsumerFailed(INatsClient client, Exception exception)
        {
            Client = client;
            Exception = exception;
        }
    }
}