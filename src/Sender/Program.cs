using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MyNatsClient;
using NATS.Client;
using SampleModel;

namespace Sender
{
    class Program
    {
        private static readonly Random Rnd = new Random();

        static void Main(string[] args)
        {
            var connectionInfo = new ConnectionInfo(SampleSettings.Hosts)
            {
                //Credentials = SampleSettings.Credentials,
                AutoRespondToPing = true,
                Verbose = false,
                PubFlushMode = PubFlushMode.Manual
            };

            using (var client = new NatsClient("mySender1", connectionInfo))
            {
                client.Connect();

                //RunManyInParallel(client);

                //SeqSend(client);

                TimeSend(client, connectionInfo.PubFlushMode == PubFlushMode.Manual);
            }

            //var cf = new ConnectionFactory();
            //using (var cn = cf.CreateConnection("nats://ubuntu01:4222"))
            //{
            //    TimeSendOfficial(cn);
            //}
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

        private static void TimeSend(NatsClient client, bool manualFlush)
        {
            Console.WriteLine("Hit key to start send.");
            Console.ReadKey();

            var sw = new Stopwatch();
            var body = new string('a', SampleSettings.TimedSample.BodyCharSize);
            var timings = new List<double>(SampleSettings.TimedSample.NumOfBatches);

            for (var n = 0; n < SampleSettings.TimedSample.NumOfBatches; n++)
            {
                sw.Restart();
                //client.PubMany(p =>
                //{
                //    for (var c = 0; c < SampleSettings.TimedSample.BatchSize; c++)
                //        p.Pub("foo", body);
                //});

                for (var c = 0; c < SampleSettings.TimedSample.BatchSize; c++)
                    client.Pub("foo", body);
                if (manualFlush)
                    client.Flush();

                sw.Stop();
                Console.Write(".");
                timings.Add(sw.Elapsed.TotalMilliseconds);
                Thread.Sleep(500);
            }
            Console.WriteLine();
            TimedInfo.Report("sender", timings, SampleSettings.TimedSample.BatchSize, SampleSettings.TimedSample.BodyCharSize);
        }

        private static void TimeSendOfficial(IConnection cn)
        {
            Console.WriteLine("Hit key to start send.");
            Console.ReadKey();

            var sw = new Stopwatch();
            var body = new string('a', SampleSettings.TimedSample.BodyCharSize);
            var timings = new List<double>(SampleSettings.TimedSample.NumOfBatches);

            for (var n = 0; n < SampleSettings.TimedSample.NumOfBatches; n++)
            {
                sw.Restart();
                for (var c = 0; c < SampleSettings.TimedSample.BatchSize; c++)
                    cn.Publish("foo", Encoding.UTF8.GetBytes(body));
                sw.Stop();
                Console.Write(".");
                timings.Add(sw.Elapsed.TotalMilliseconds);
                Thread.Sleep(500);
            }
            Console.WriteLine();
            TimedInfo.Report("sender", timings, SampleSettings.TimedSample.BatchSize, SampleSettings.TimedSample.BodyCharSize);
        }
    }
}
