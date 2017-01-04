using System.Threading.Tasks;

namespace MyNatsClient
{
    public interface INatsStreamWriter
    {
        void Flush();
        Task FlushAsync();
        void Write(byte[] data);
        void Write(IPayload data);
        Task WriteAsync(byte[] data);
        Task WriteAsync(IPayload data);
    }
}