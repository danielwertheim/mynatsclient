namespace MyNatsClient.Ops
{
    public sealed class PongOp : IOp
    {
        public const string Name = "PONG";

        public static readonly PongOp Instance = new PongOp();

        private PongOp() { }

        public string GetAsString() => Name;
    }
}