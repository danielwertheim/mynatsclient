namespace MyNatsClient.Ops
{
    public sealed class OkOp : IOp
    {
        internal const string OpMarker = "+OK";

        public string Marker => OpMarker;

        public static readonly OkOp Instance = new();

        private OkOp() { }

        public override string ToString()
            => OpMarker;
    }
}
