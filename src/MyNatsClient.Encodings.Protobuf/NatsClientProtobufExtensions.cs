using System.Threading.Tasks;

namespace MyNatsClient.Encodings.Protobuf
{
    public static class NatsClientProtobufExtensions
    {
        public static void PubAsProtobuf<TItem>(this INatsClient client, string subject, TItem item, string replyTo = null) where TItem : class
        {
            var payload = ProtobufEncoding.Default.Encode(item);

            client.Pub(subject, payload, replyTo);
        }

        public static async Task PubAsProtobufAsync<TItem>(this INatsClient client, string subject, TItem item, string replyTo = null) where TItem : class
        {
            var payload = ProtobufEncoding.Default.Encode(item);

            await client.PubAsync(subject, payload, replyTo).ConfigureAwait(false);
        }
    }
}