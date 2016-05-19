namespace NatsFun.Events
{
    public class ClientDisconnected : IClientEvent
    {
        public INatsClient Client { get; }
        public DisconnectReason Reason { get; }

        public ClientDisconnected(INatsClient client, DisconnectReason reason)
        {
            Client = client;
            Reason = reason;
        }
    }
}