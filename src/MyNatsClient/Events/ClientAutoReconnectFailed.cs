namespace MyNatsClient.Events
{
    public class ClientAutoReconnectFailed : IClientEvent
    {
        public INatsClient Client { get; }

        public ClientAutoReconnectFailed(INatsClient client)
        {
            Client = client;
        }
    }
}