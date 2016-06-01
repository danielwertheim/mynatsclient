using System.Net.Sockets;

namespace MyNatsClient
{
    public interface ISocketFactory
    {
        Socket Create();
    }
}