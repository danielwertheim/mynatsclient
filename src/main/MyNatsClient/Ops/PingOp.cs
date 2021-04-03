namespace MyNatsClient.Ops
{
    public sealed class PingOp : IOp
    {
        internal const string OpMarker = "PING";

        public string Marker => OpMarker;

        public static readonly PingOp Instance = new();

        private PingOp() { }

        public override string ToString()
            => OpMarker;
    }
}
