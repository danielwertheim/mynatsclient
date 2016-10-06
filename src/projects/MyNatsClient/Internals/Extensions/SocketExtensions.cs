using System.IO;
using System.Net.Sockets;

namespace MyNatsClient.Internals.Extensions
{
    internal static class SocketExtensions
    {
        internal static NetworkStream CreateReadStream(this Socket socket)
        {
#if NETSTANDARD1_6
            var s = new NetworkStream(socket, false);
#else
            var s = new NetworkStream(socket, FileAccess.Read, false);
#endif
            if (socket.ReceiveTimeout > 0)
                s.ReadTimeout = socket.ReceiveTimeout;

            return s;
        }

        internal static NetworkStream CreateWriteStream(this Socket socket)
        {
#if NETSTANDARD1_6
            var s = new NetworkStream(socket, false);
#else
            var s = new NetworkStream(socket, FileAccess.Write, false);
#endif
            if (socket.SendTimeout > 0)
                s.WriteTimeout = socket.SendTimeout;

            return s;
        }
    }
}