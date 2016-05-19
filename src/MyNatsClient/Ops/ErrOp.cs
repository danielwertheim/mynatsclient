namespace NatsFun.Ops
{
    public class ErrOp : IOp
    {
        public string Code => "-ERR";
        public string Message { get; }

        public ErrOp(string message)
        {
            Message = message;
        }

        public string GetAsString()
        {
            return $"-ERR {Message}\r\n";
        }
    }
}