using MyNatsClient.Ops;
using Newtonsoft.Json;

namespace MyNatsClient.Internals
{
    internal class NatsServerInfo
    {
        [JsonProperty("auth_required")]
        public bool AuthRequired { get; set; }

        [JsonProperty("max_payload")]
        public long MaxPayload { get; set; }

        private NatsServerInfo() { }

        public static NatsServerInfo Parse(InfoOp op)
        {
            return JsonConvert.DeserializeObject<NatsServerInfo>(op.Message, JsonOptions.Instance);
        }
    }
}