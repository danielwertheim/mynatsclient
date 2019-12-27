namespace MyNatsClient.Ops
{
    public sealed class ErrOp : IOp
    {
        public const string Name = "-ERR";

        public readonly string Message;

        public ErrOp(string message)
            => Message = message;

        public string GetAsString()
            => $"{Name} {Message}";
    }
}