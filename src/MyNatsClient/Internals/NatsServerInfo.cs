using Jil;
using MyNatsClient.Ops;

namespace MyNatsClient.Internals
{
    internal class NatsServerInfo
    {
        [JilDirective("auth_required")]
        public bool AuthRequired { get; set; }

        [JilDirective("max_payload")]
        public long MaxPayload { get; set; }

        private NatsServerInfo() { }

        public static NatsServerInfo Parse(InfoOp op)
        {
            return JSON.Deserialize<NatsServerInfo>(op.Message, JilOptions.Instance);
        }
    }
}