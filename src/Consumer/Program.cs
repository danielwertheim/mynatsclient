using System;
using System.Reactive.Linq;
using System.Text;
using NatsFun;
using NatsFun.Events;
using NatsFun.Ops;

namespace Consumer
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionInfo = new ConnectionInfo(
                //Client id (becomes part of subscription id)
                "myconsumer1",
                //Hosts to use. When connecting, will randomize the list
                //and try to connect. First successful will be used.
                new[]
                {
                    new Host("192.168.1.176", 4222)
                })
            {
                AutoRespondToPing = true,
                Verbose = true
            };

            using (var client = new NatsClient(connectionInfo))
            {
                //You can subscribe to dispatched client events
                //to react on something that happened to the client
                client.Events.OfType<ClientConnected>().Subscribe(ev =>
                {
                    ev.Client.Sub("foo", "s1");
                    ev.Client.Sub("bar", "s2");

                    //Make it automatically unsub after two messages
                    //client.UnSub("s1", 2);
                });

                client.Events.OfType<ClientFailed>().Subscribe(
                    ev => Console.WriteLine("Client failed!"));

                //Disconnect, either by client.Disconnect() call
                //or caused by fail.
                //No auto reconnect exists yet, you can call connect
                //and resubscribe.
                client.Events.OfType<ClientDisconnected>().Subscribe(ev =>
                {
                    Console.WriteLine($"Client was disconnected due to reason '{ev.Reason}'");
                    if (ev.Reason != DisconnectReason.DueToFailure)
                        return;

                    ev.Client.Connect();
                });

                //Subscribe to IncomingOps All or e.g InfoOp, ErrorOp, MsgOp, PingOp, PongOp.
                client.IncomingOps.Subscribe(op =>
                {
                    Console.WriteLine("===== RECEIVED =====");
                    Console.Write(op.GetAsString());
                    Console.WriteLine($"OpCount: {client.Stats.OpCount}");
                });

                client.IncomingOps.OfType<PingOp>().Subscribe(ping =>
                {
                    if (!connectionInfo.AutoRespondToPing)
                        client.Pong();
                });

                client.IncomingOps.OfType<MsgOp>().Subscribe(msg =>
                {
                    Console.WriteLine("===== MSG =====");
                    Console.WriteLine($"Subject: {msg.Subject}");
                    Console.WriteLine($"QueueGroup: {msg.QueueGroup}");
                    Console.WriteLine($"SubscriptionId: {msg.SubscriptionId}");
                    Console.WriteLine($"Payload: {Encoding.UTF8.GetString(msg.Payload)}");

                    if (Encoding.UTF8.GetString(msg.Payload) == "FAIL")
                        client.Send("FAIL");
                });

                client.Connect();
                
                Console.WriteLine("Hit key to UnSub from foo.");
                Console.ReadKey();
                client.UnSub("s1");

                Console.WriteLine("Hit key to Disconnect.");
                Console.ReadKey();
                client.Disconnect();

                Console.WriteLine("Hit key to Connect.");
                Console.ReadKey();
                client.Connect();

                Console.WriteLine("Hit key to Shutdown.");
                Console.ReadKey();
            }
        }
    }
}
