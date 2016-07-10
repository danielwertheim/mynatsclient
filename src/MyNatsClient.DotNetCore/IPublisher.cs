using System.Threading.Tasks;

namespace MyNatsClient
{
    public interface IPublisher
    {
        void Pub(string subject, string body, string replyTo = null);
        void Pub(string subject, byte[] body, string replyTo = null);
        void Pub(string subject, IPayload body, string replyTo = null);

        Task PubAsync(string subject, string body, string replyTo = null);
        Task PubAsync(string subject, byte[] body, string replyTo = null);
        Task PubAsync(string subject, IPayload body, string replyTo = null);
    }
}