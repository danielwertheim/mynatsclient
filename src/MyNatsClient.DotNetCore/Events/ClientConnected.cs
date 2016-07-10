namespace MyNatsClient.Events
{
    public class ClientConnected : IClientEvent
    {
        public INatsClient Client { get; }

        public ClientConnected(INatsClient client)
        {
            Client = client;
        }
    }
}