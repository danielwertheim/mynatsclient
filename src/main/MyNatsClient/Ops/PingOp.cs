namespace MyNatsClient.Ops
{
    public sealed class PingOp : IOp
    {
        public const string Name = "PING";

        public static readonly PingOp Instance = new PingOp();

        private PingOp() { }

        public string GetAsString() => Name;
    }
}