namespace NatsFun
{
    public class Host
    {
        public string Address { get; }
        public int Port { get; }

        public Host(string address, int port)
        {
            Address = address;
            Port = port;
        }
    }
}