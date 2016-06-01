using System.Net.Sockets;

namespace MyNatsClient.Internals
{
    internal class SocketFactory : ISocketFactory
    {
        private const int DefaultReceiveBufferSize = 32768 * 2;
        private const int DefaultSendBufferSize = 32768;

        public Socket Create()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                ReceiveBufferSize = DefaultReceiveBufferSize,
                SendBufferSize = DefaultSendBufferSize
            };

            return socket;
        }
    }
}