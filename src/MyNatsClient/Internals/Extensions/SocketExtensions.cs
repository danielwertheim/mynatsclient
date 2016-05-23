using System.Net.Sockets;
using System.Text;

namespace MyNatsClient.Internals.Extensions
{
    internal static class SocketExtensions
    {
        internal static void SendUtf8(this Socket socket, string data)
            => socket.Send(Encoding.UTF8.GetBytes(data));
    }
}