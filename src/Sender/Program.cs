using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MyNatsClient;

namespace Sender
{
    class Program
    {
        private static readonly Random Rnd = new Random();

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
                Credentials = new Credentials("test", "p@ssword1234")
            };

            using (var client = new NatsClient("mySender1", connectionInfo))
            {
                client.Connect();

                //RunManyInParallel(client);

                //SeqSend(client);

                //TimeSend(client, 10, 100000);
            }
        }

        private static void SeqSend(NatsClient client)
        {
            while (true)
            {
                Console.WriteLine("cmd[q,s]:>>");
                var c = Console.ReadKey();
                if (c.KeyChar == 'q')
                    break;

                Console.WriteLine();
                if (c.KeyChar == 's')
                {
                    var run = true;
                    var t = Task.Run(() =>
                    {
                        var n = 0;

                        while (run)
                        {
                            n += 1;
                            var msg = $"Test{n}\r\nwith two lines!!!";
                            Console.WriteLine($"Publishing '{msg}' to 'foo'");
                            client.Pub("foo", msg);
                            Thread.Sleep(Rnd.Next(500, 2500));
                        }
                    });

                    Console.WriteLine("Hit key to stop.");
                    Console.ReadKey();
                    run = false;
                    t.Wait();
                }
            }
        }

        private static void RunManyInParallel(NatsClient client)
        {
            var cancellation = new CancellationTokenSource();

            var t1 = Task.Run(async () =>
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

            var t2 = Task.Run(async () =>
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

            var t3 = Task.Run(() =>
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

            var t4 = Task.Run(() =>
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
            Task.WaitAll(t1, t2, t3, t4);
        }

        private static void TimeSend(NatsClient client, int nBatches, int batchSize)
        {
            Console.WriteLine("Hit key to start send.");
            Console.ReadKey();

            var sw = new Stopwatch();

            for (var n = 0; n < nBatches; n++)
            {
                sw.Restart();
                for (var c = 0; c < batchSize; c++)
                    client.Pub("foo", $"{n}.{c}");
                sw.Stop();
                Console.WriteLine(sw.Elapsed.TotalMilliseconds);
                Thread.Sleep(500);
            }
        }
    }
}
