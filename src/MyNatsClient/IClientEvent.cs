namespace MyNatsClient
{
    public interface IClientEvent
    {
        INatsClient Client { get; }
    }
}