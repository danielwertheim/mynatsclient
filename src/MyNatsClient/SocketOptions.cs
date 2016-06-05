namespace MyNatsClient
{
    public class SocketOptions
    {
        public int? ReceiveBufferSize { get; set; }
        public int? SendBufferSize { get; set; }
        public int? ReceiveTimeoutMs { get; set; } = 10000;
        public int? SendTimeoutMs { get; set; }
    }
}