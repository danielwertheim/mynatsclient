namespace MyNatsClient.Ops
{
    public class OkOp : IOp
    {
        public static readonly OkOp Instance = new OkOp();

        public string Code => "+OK";

        private OkOp() { }

        public string GetAsString() => "+OK\r\n";
    }
}