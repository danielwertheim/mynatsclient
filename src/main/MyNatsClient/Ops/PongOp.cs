namespace MyNatsClient.Ops
{
    public sealed class PongOp : IOp
    {
        internal const string OpMarker = "PONG";

        public string Marker => OpMarker;

        public static readonly PongOp Instance = new();

        private PongOp() { }

        public override string ToString()
            => OpMarker;
    }
}
