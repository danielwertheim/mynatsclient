using System.Threading.Tasks;

namespace MyNatsClient.Encodings.Json
{
    public static class NatsClientJsonExtensions
    {
        public static void PubAsJson<TItem>(this INatsClient client, string subject, TItem item, string replyTo = null) where TItem : class
        {
            var payload = JsonEncoding.Default.Encode(item);

            client.Pub(subject, payload, replyTo);
        }

        public static async Task PubAsJsonAsync<TItem>(this INatsClient client, string subject, TItem item, string replyTo = null) where TItem : class
        {
            var payload = JsonEncoding.Default.Encode(item);

            await client.PubAsync(subject, payload, replyTo).ConfigureAwait(false);
        }
    }
}