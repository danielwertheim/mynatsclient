namespace MyNatsClient.Ops
{
    public class PingOp : IOp
    {
        public static readonly PingOp Instance = new PingOp();

        public string Code => "PING";

        private PingOp() { }

        public string GetAsString() => Code;
    }
}