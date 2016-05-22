using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NatsFun;
using NatsFun.Ops;

namespace Sender
{
    class Program
    {
        private static readonly Random Rnd = new Random();

        static void Main(string[] args)
        {
            var connectionInfo = new ConnectionInfo(
                //Client id (becomes part of subscription id)
                "mySender1",
                //Hosts to use. When connecting, will randomize the list
                //and try to connect. First successful will be used.
                new[]
                {
                    new Host("192.168.1.176", 4222)
                });

            using (var client = new NatsClient(connectionInfo))
            {
                client.Connect();
                client.IncomingOps.OfType<PingOp>().Subscribe(ping => client.Pong());

                var cancellation = new CancellationTokenSource();

                Task.Run(async () =>
                {
                    var i = 0;
                    while (!cancellation.IsCancellationRequested)
                    {
                        await client.PubAsync("foo", $"async1.{i}").ConfigureAwait(false);
                        await Task.Delay(Rnd.Next(10, 100), cancellation.Token).ConfigureAwait(false);
                        i += 1;
                    }
                }, cancellation.Token).ContinueWith(t =>
                {
                    if (t.Exception != null)
                        Console.WriteLine(t.Exception.GetBaseException());
                }, cancellation.Token);

                Task.Run(async () =>
                {
                    var i = 0;
                    while (!cancellation.IsCancellationRequested)
                    {
                        await client.PubAsync("foo", $"async2.{i}").ConfigureAwait(false);
                        await Task.Delay(Rnd.Next(10, 100), cancellation.Token).ConfigureAwait(false);
                        i += 1;
                    }
                }, cancellation.Token).ContinueWith(t =>
                {
                    if (t.Exception != null)
                        Console.WriteLine(t.Exception.GetBaseException());
                }, cancellation.Token);

                Task.Run(() =>
                {
                    var i = 0;
                    while (!cancellation.IsCancellationRequested)
                    {
                        client.Pub("foo", $"sync1.{i}");
                        Thread.Sleep(Rnd.Next(10, 100));
                        i += 1;
                    }
                }, cancellation.Token).ContinueWith(t =>
                {
                    if (t.Exception != null)
                        Console.WriteLine(t.Exception.GetBaseException());
                }, cancellation.Token);

                Task.Run(() =>
                {
                    var i = 0;
                    while (!cancellation.IsCancellationRequested)
                    {
                        client.Pub("foo", $"sync2.{i}");
                        Thread.Sleep(Rnd.Next(10, 100));
                        i += 1;
                    }
                }, cancellation.Token).ContinueWith(t =>
                {
                    if (t.Exception != null)
                        Console.WriteLine(t.Exception.GetBaseException());
                }, cancellation.Token);

                Console.ReadKey();
                cancellation.Cancel();
                Console.ReadKey();
                //while (true)
                //{
                //    Console.WriteLine("cmd[q,s]:>>");
                //    var c = Console.ReadKey();
                //    if (c.KeyChar == 'q')
                //        break;

                //    Console.WriteLine();
                //    if (c.KeyChar == 's')
                //    {
                //        var run = true;
                //        var t = Task.Run(() =>
                //        {
                //            var n = 0;

                //            while (run)
                //            {
                //                n += 1;
                //                var msg = $"Test{n}\r\nwith two lines!!!";
                //                Console.WriteLine($"Publishing '{msg}' to 'foo'");
                //                client.Pub("foo", msg);
                //                Thread.Sleep(Rnd.Next(500, 2500));
                //            }
                //        });

                //        Console.WriteLine("Hit key to stop.");
                //        Console.ReadKey();
                //        run = false;
                //        t.Wait();
                //    }
                //}
            }
        }
    }
}
