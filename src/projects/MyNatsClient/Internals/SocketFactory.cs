using System.Net.Sockets;

namespace MyNatsClient.Internals
{
    internal class SocketFactory : ISocketFactory
    {
        public Socket Create(SocketOptions options)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (options.ReceiveBufferSize.HasValue)
                socket.ReceiveBufferSize = options.ReceiveBufferSize.Value;

            if (options.SendBufferSize.HasValue)
                socket.SendBufferSize = options.SendBufferSize.Value;

            if (options.ReceiveTimeoutMs.HasValue)
                socket.ReceiveTimeout = options.ReceiveTimeoutMs.Value;

            if (options.SendTimeoutMs.HasValue)
                socket.SendTimeout = options.SendTimeoutMs.Value;

            return socket;
        }
    }
}