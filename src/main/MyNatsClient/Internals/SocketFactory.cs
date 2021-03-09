using System.Net.Sockets;

namespace MyNatsClient.Internals
{
    internal class SocketFactory : ISocketFactory
    {
        private static AddressFamily GetAddressFamily(SocketAddressType addressType)
            => addressType == SocketAddressType.IpV6
                ? AddressFamily.InterNetworkV6
                : AddressFamily.InterNetwork;

        public Socket Create(SocketOptions options)
        {
            var socket = !options.AddressType.HasValue
                ? new Socket(SocketType.Stream, ProtocolType.Tcp)
                : new Socket(GetAddressFamily(options.AddressType.Value), SocketType.Stream, ProtocolType.Tcp);

            if (options.UseNagleAlgorithm.HasValue)
                socket.NoDelay = !options.UseNagleAlgorithm.Value;

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