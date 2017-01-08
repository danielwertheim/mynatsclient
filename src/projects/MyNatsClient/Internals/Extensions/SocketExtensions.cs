using System;
using System.IO;
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

#if !NETSTANDARD1_6
            var connectTask = Task.Run(() =>
            {
                var r = socket.BeginConnect(endPoint, null, null);
                if (r.AsyncWaitHandle.WaitOne(timeoutMs, true))
                    return;

                throw NatsException.FailedToConnectToHost(
                    host, $"Socket could not connect against {host}, within specified timeout {timeoutMs.ToString()}ms.");
            }, cancellationToken);

            if (connectTask.Wait(timeoutMs, cancellationToken))
                return;

            if (connectTask.IsFaulted)
            {
                var ex = connectTask.Exception?.GetBaseException();
                if (ex != null)
                    throw ex;
            }
#else
            var connectTask = socket.ConnectAsync(endPoint);
            if (connectTask.Wait(timeoutMs, cancellationToken))
                return;
#endif
            throw NatsException.FailedToConnectToHost(
                host, $"Socket could not connect against {host}, within specified timeout {timeoutMs.ToString()}ms.");

        }

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