namespace NatsFun.Ops
{
    public class PingOp : IOp
    {
        public static readonly PingOp Instance = new PingOp();

        public string Code => "PING";

        private PingOp() { }

        public string GetAsString()
        {
            return "PING\r\n";
        }
    }
}