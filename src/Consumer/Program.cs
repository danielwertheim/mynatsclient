using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using MyNatsClient;
using MyNatsClient.Events;
using MyNatsClient.Ops;
using NATS.Client;
using SampleModel;

namespace Consumer
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionInfo = new ConnectionInfo(SampleSettings.Hosts)
            {
                //Credentials = SampleSettings.Credentials,
                AutoRespondToPing = true,
                Verbose = false
            };

            //FeaturesSample(connectionInfo);
            TimedSample(connectionInfo);

            //TimedSampleOfficial();
        }

        private static void FeaturesSample(ConnectionInfo connectionInfo)
        {
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

                //Subscribe to OpStream All or e.g InfoOp, ErrorOp, MsgOp, PingOp, PongOp.
                client.OpStream.Subscribe(op =>
                {
                    Console.WriteLine("===== RECEIVED =====");
                    Console.WriteLine(op.GetAsString());
                    Console.WriteLine($"OpCount: {client.Stats.OpCount}");
                });

                client.OpStream.OfType<PingOp>().Subscribe(async ping =>
                {
                    if (!connectionInfo.AutoRespondToPing)
                        await client.PongAsync();
                });

                client.MsgOpStream.Subscribe(msg =>
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

        private static void TimedSample(ConnectionInfo connectionInfo)
        {
            using (var client = new NatsClient("myconsumer1", connectionInfo))
            {
                client.Events.OfType<ClientConnected>().Subscribe(
                    async ev => await ev.Client.SubAsync("foo", "s1"));
                client.Events.OfType<ClientConsumerFailed>().Subscribe(
                    ev => Console.WriteLine("Client consumer failed!" + ev.Exception));
                client.Events.OfType<ClientDisconnected>().Subscribe(
                    ev => Console.WriteLine($"Client was disconnected due to reason '{ev.Reason}'"));

                var sw = new Stopwatch();
                var n = 0;
                var timings = new List<double>(SampleSettings.TimedSample.NumOfBatches);
                client.MsgOpStream.Subscribe(msg =>
                {
                    n++;
                    if (!sw.IsRunning)
                        sw.Start();

                    if (n == SampleSettings.TimedSample.BatchSize)
                    {
                        sw.Stop();
                        timings.Add(sw.Elapsed.TotalMilliseconds);
                        sw.Reset();
                        n = 0;
                    }
                });

                //var dump = File.CreateText(@"d:\temp\log.txt");
                //client.MsgOpStream.Subscribe(msg =>
                //{
                //    dump.WriteLine(msg.GetAsString());
                //});

                client.Connect();

                Console.WriteLine("Hit key to show timings.");
                Console.ReadKey();
                //dump.Flush();
                //dump.Close();
                TimedInfo.Report("consumer", timings, SampleSettings.TimedSample.BatchSize, SampleSettings.TimedSample.BodyCharSize);

                Console.WriteLine("Hit key to Shutdown.");
                Console.ReadKey();
            }
        }

        private static void TimedSampleOfficial()
        {
            var sw = new Stopwatch();
            var n = 0;
            var timings = new List<double>(SampleSettings.TimedSample.NumOfBatches);

            var cf = new ConnectionFactory();
            var opts = ConnectionFactory.GetDefaultOptions();
            opts.Verbose = false;
            opts.Servers = new[] {"nats://ubuntu01:4222"};
            using (var cn = cf.CreateConnection(opts))
            {
                EventHandler<MsgHandlerEventArgs> h = (sender, args) =>
                {
                    n++;
                    if (!sw.IsRunning)
                        sw.Start();

                    if (n == SampleSettings.TimedSample.BatchSize)
                    {
                        sw.Stop();
                        timings.Add(sw.Elapsed.TotalMilliseconds);
                        sw.Reset();
                        n = 0;
                    }
                };

                var s = cn.SubscribeAsync("foo", h);

                //var dump = File.CreateText(@"d:\temp\log.txt");
                //client.MsgOpStream.Subscribe(msg =>
                //{
                //    dump.WriteLine(msg.GetAsString());
                //});

                Console.WriteLine("Hit key to show timings.");
                Console.ReadKey();
                //dump.Flush();
                //dump.Close();

                TimedInfo.Report("consumer", timings, SampleSettings.TimedSample.BatchSize, SampleSettings.TimedSample.BodyCharSize);

                Console.WriteLine("Hit key to Shutdown.");
                Console.ReadKey();
                cn.Close();
            }
        }
    }
}
