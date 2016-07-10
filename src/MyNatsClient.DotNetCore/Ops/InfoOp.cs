namespace MyNatsClient.Ops
{
    public class InfoOp : IOp
    {
        public string Code => "INFO";
        public string Message { get; }

        public InfoOp(string message)
        {
            Message = message;
        }

        public string GetAsString() => $"INFO {Message}";
    }
}