using System.Net.Sockets;

namespace MyNatsClient.Internals
{
    internal static class SocketFactory
    {
        internal static Socket Create()
        {
            //TODO: Tweaks for timeouts, buffer sizes etc
            return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
    }
}