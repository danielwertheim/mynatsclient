namespace MyNatsClient
{
    public interface IEncoder
    {
        IPayload Encode<TItem>(TItem item) where TItem : class;
    }
}