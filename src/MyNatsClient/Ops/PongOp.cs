namespace NatsFun.Ops
{
public class PongOp : IOp
{
    public static readonly PongOp Instance = new PongOp();

    public string Code => "PONG";

    private PongOp() { }

    public string GetAsString()
    {
        return "PONG\r\n";
    }
}
}