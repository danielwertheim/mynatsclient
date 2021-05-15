using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MyNatsClient.Internals.Extensions
{
    internal static class SocketExtensions
    {
        internal static async Task ConnectAsync(this Socket socket, Host host, int timeoutMs, CancellationToken cancellationToken)
        {
            var endPoint = new DnsEndPoint(host.Address, host.Port);

            var connectTask = socket.ConnectAsync(endPoint);

            await Task.WhenAny(Task.Delay(timeoutMs, cancellationToken), connectTask).ConfigureAwait(false);

            //var connectedOk = socket.Connected && connectTask.IsCompletedSuccessfully;
            //if (connectedOk)
            //    return;

            if (socket.Connected)
                return;

            socket.Close();

            throw NatsException.FailedToConnectToHost(
                host, $"Socket could not connect against {host}, within specified timeout {timeoutMs.ToString()}ms.");
        }

        internal static NetworkStream CreateReadStream(this Socket socket)
        {
            var ns = new NetworkStream(socket, FileAccess.Read, false);

            if (socket.ReceiveTimeout > 0)
                ns.ReadTimeout = socket.ReceiveTimeout;

            return ns;
        }

        internal static NetworkStream CreateWriteStream(this Socket socket)
        {
            var ns = new NetworkStream(socket, FileAccess.Write, false);

            if (socket.SendTimeout > 0)
                ns.WriteTimeout = socket.SendTimeout;

            return ns;
        }

        internal static NetworkStream CreateReadWriteStream(this Socket socket)
        {
            var ns = new NetworkStream(socket, FileAccess.ReadWrite, false);

            if (socket.SendTimeout > 0)
                ns.WriteTimeout = socket.SendTimeout;

            if (socket.ReceiveTimeout > 0)
                ns.ReadTimeout = socket.ReceiveTimeout;

            return ns;
        }
    }
}
