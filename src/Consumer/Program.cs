using System;
using System.Reactive.Linq;
using System.Text;
using MyNatsClient;
using MyNatsClient.Events;
using MyNatsClient.Ops;

namespace Consumer
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionInfo = new ConnectionInfo(
                //Hosts to use. When connecting, will randomize the list
                //and try to connect. First successful will be used.
                new[]
                {
                    new Host("ubuntu01", 4223)
                })
            {
                Credentials = new Credentials("test", "p@ssword1234"),
                AutoRespondToPing = true,
                Verbose = true
            };

            using (var client = new NatsClient("myconsumer1", connectionInfo))
            {
                //You can subscribe to dispatched client events
                //to react on something that happened to the client
                client.Events.OfType<ClientConnected>().Subscribe(async ev =>
                {
                    Console.WriteLine("Client connected");
                    await ev.Client.SubAsync("foo", "s1");
                    await ev.Client.SubAsync("foo", "s2");
                    await ev.Client.SubAsync("bar", "s3");

                    //Make it automatically unsub after two messages
                    await ev.Client.UnSubAsync("s2", 2);
                });

                client.Events.OfType<ClientConsumerFailed>().Subscribe(
                    ev => Console.WriteLine("Client consumer failed!"));

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
                    Console.WriteLine(op.GetAsString());
                    Console.WriteLine($"OpCount: {client.Stats.OpCount}");
                });

                client.IncomingOps.OfType<PingOp>().Subscribe(async ping =>
                {
                    if (!connectionInfo.AutoRespondToPing)
                        await client.PongAsync();
                });

                client.IncomingOps.OfType<MsgOp>().Subscribe(msg =>
                {
                    Console.WriteLine("===== MSG =====");
                    Console.WriteLine($"Subject: {msg.Subject}");
                    Console.WriteLine($"QueueGroup: {msg.QueueGroup}");
                    Console.WriteLine($"SubscriptionId: {msg.SubscriptionId}");
                    Console.WriteLine($"Payload: {Encoding.UTF8.GetString(msg.Payload)}");
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
