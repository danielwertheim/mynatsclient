using System.Threading.Tasks;

namespace MyNatsClient
{
    internal interface INatsStreamWriter
    {
        void Flush();
        Task FlushAsync();
        void Write(byte[] data);
        void Write(IPayload data);
        Task WriteAsync(byte[] data);
        Task WriteAsync(IPayload data);
    }
}