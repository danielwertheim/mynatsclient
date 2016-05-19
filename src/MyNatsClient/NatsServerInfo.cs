using Jil;
using NatsFun.Internals;
using NatsFun.Ops;

namespace NatsFun
{
    public class NatsServerInfo
    {
        [JilDirective("max_payload")]
        public long MaxPayload { get; set; }

        private NatsServerInfo() { }

        public static NatsServerInfo Parse(InfoOp op)
        {
            return JSON.Deserialize<NatsServerInfo>(op.Message, JilOptions.Instance);
        }
    }
}