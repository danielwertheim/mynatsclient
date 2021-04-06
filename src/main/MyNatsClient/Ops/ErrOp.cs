namespace MyNatsClient.Ops
{
    public sealed class ErrOp : IOp
    {
        internal const string OpMarker = "-ERR";

        public string Marker => OpMarker;

        public readonly string Message;

        public ErrOp(string message)
            => Message = message;

        public override string ToString()
            => OpMarker;
    }
}
