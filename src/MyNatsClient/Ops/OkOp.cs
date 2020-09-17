namespace MyNatsClient.Ops
{
    public sealed class OkOp : IOp
    {
        public const string Name = "+OK";

        public static readonly OkOp Instance = new OkOp();

        private OkOp() { }

        public string GetAsString() => Name;
    }
}