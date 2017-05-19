using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MyNatsClient.Internals.Extensions
{
    internal static class SocketExtensions
    {
        internal static void Connect(this Socket socket, Host host, int timeoutMs, CancellationToken cancellationToken)
        {
            var endPoint = new DnsEndPoint(host.Address, host.Port);

#if NET451
            var connectTask = Task.Factory.FromAsync(
                socket.BeginConnect(endPoint, null, null),
                _ => { });
#else
            var connectTask = socket.ConnectAsync(endPoint);
#endif

            var firstCompletedTask = Task.WhenAny(connectTask, Task.Delay(timeoutMs, cancellationToken));
            if (firstCompletedTask == connectTask && connectTask.IsCompleted)
                return;

            throw NatsException.FailedToConnectToHost(
                host, $"Socket could not connect against {host}, within specified timeout {timeoutMs.ToString()}ms.");
        }

        internal static NetworkStream CreateReadStream(this Socket socket)
        {
            var s = new NetworkStream(socket, false);

            if (socket.ReceiveTimeout > 0)
                s.ReadTimeout = socket.ReceiveTimeout;

            return s;
        }

        internal static NetworkStream CreateWriteStream(this Socket socket)
        {
            var s = new NetworkStream(socket, false);

            if (socket.SendTimeout > 0)
                s.WriteTimeout = socket.SendTimeout;

            return s;
        }
    }
}