using System;
using System.Threading.Tasks;

namespace MyNatsClient
{
    public interface IPublisher
    {
        void Pub(string subject, string body, string replyTo = null);
        void Pub(string subject, ReadOnlyMemory<byte> body, string replyTo = null);

        Task PubAsync(string subject, string body, string replyTo = null);
        Task PubAsync(string subject, ReadOnlyMemory<byte> body, string replyTo = null);
    }
}