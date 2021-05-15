namespace MyNatsClient.Ops
{
    public sealed class NullOp : IOp
    {
        private const string OpMarker = "NULL";

        public string Marker => OpMarker;

        public static readonly NullOp Instance = new();

        private NullOp() { }

        public override string ToString()
            => OpMarker;
    }
}
