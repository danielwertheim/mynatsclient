using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyNatsClient;
using MyNatsClient.Rx;
using NATS.Client;

namespace Benchmarks
{
    public class Program
    {
        private const int N = 100000;
        private const int Size = 256;

        private static ReadOnlyMemory<byte> GeneratePayload()
        {
            var p = new Memory<byte>(new byte[Size]);
            var w = p.Span;
            var bs = new[] {(byte) 'a', (byte) 'b'};
            for (var i = 0; i < Size; i++)
                w[i] = bs[i % 2];

            return p;
        }

        public static async Task Main(string[] args)
        {
            var testCase = args.FirstOrDefault();

            Task<TimeSpan> task;

            switch (testCase?.ToLowerInvariant())
            {
                case "a":
                    task = Scenarios.MyNatsClientRequestAsync(N, GeneratePayload(), false);
                    break;
                case "asec":
                    task = Scenarios.MyNatsClientRequestAsync(N, GeneratePayload(), true);
                    break;
                case "b":
                    task = Scenarios.NatsClientRequestAsync(N, GeneratePayload(), false);
                    break;
                case "bsec":
                    task = Scenarios.NatsClientRequestAsync(N, GeneratePayload(), true);
                    break;
                case "o":
                    task = Scenarios.OldMyNatsClientRequestAsync(N, GeneratePayload(), false);
                    break;
                default:
                    Console.WriteLine("Select a scenario: [o,b]");
                    return;
            }

            var elapsed = await task.ConfigureAwait(false);

            Console.WriteLine($"Elapsed {elapsed}");
            Console.WriteLine($"{N / elapsed.TotalSeconds} msg/s");
        }
    }

    public static class Scenarios
    {
        public static async Task<TimeSpan> MyNatsClientRequestAsync(int n, ReadOnlyMemory<byte> payload, bool useTls)
        {
            Console.WriteLine($"MyNatsClient-RequestAsync: n={n} s={payload.Length}");

            const string subject = "casea";
            using var cts = new CancellationTokenSource();
            using var sync = new AutoResetEvent(false);
            var tcs = new TaskCompletionSource<TimeSpan>();
            var cnInfo = new ConnectionInfo("127.0.0.1");

            if (useTls)
                cnInfo.ServerCertificateValidation = (_, __, ___) => true;

            var responderTask = Task.Factory.StartNew(async () =>
            {
                using var client = new NatsClient(cnInfo);
                await client.ConnectAsync().ConfigureAwait(false);

                client.Sub(subject, messages => messages.SubscribeSafe(msg => { client.Pub(msg.ReplyTo, msg.Payload); }));

                sync.Set();

                await tcs.Task.ConfigureAwait(false);
            }, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            var requesterTask = Task.Factory.StartNew(async () =>
            {
                using var client = new NatsClient(cnInfo);

                await client.ConnectAsync().ConfigureAwait(false);

                if(!sync.WaitOne(1000))
                    throw new Exception("Responder does not seem to be started.");

                for (var i = 0; i < 10; i++)
                    await client.RequestAsync(subject, payload, cts.Token).ConfigureAwait(false);

                var sw = Stopwatch.StartNew();

                for (var i = 0; i < n; i++)
                    await client.RequestAsync(subject, payload, cts.Token).ConfigureAwait(false);

                sw.Stop();
                tcs.SetResult(sw.Elapsed);
            }, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            var elapsed = await tcs.Task.ConfigureAwait(false);

            cts.Cancel();

            await Task.WhenAll(responderTask, requesterTask).ConfigureAwait(false);

            return elapsed;
        }

        public static async Task<TimeSpan> NatsClientRequestAsync(int n, ReadOnlyMemory<byte> payload, bool useTls)
        {
            Console.WriteLine($"Official NatsClient-RequestAsync: n={n} s={payload.Length}");

            const string subject = "caseb";
            using var cts = new CancellationTokenSource();
            using var sync = new AutoResetEvent(false);
            var tcs = new TaskCompletionSource<TimeSpan>();

            var cf = new ConnectionFactory();
            var opts = ConnectionFactory.GetDefaultOptions();
            opts.Url = "nats://127.0.0.1:4222";
            opts.Secure = useTls;

            if(useTls)
                opts.TLSRemoteCertificationValidationCallback = (_, __, ___, ____) => true;

            var body = payload.ToArray();

            var responderTask = Task.Factory.StartNew(async () =>
            {
                using var cn = cf.CreateConnection(opts);

                cn.SubscribeAsync(subject, (_, args) =>
                {
                    args.Message.Respond(args.Message.Data);
                    args.Message.ArrivalSubcription.Connection.Flush();
                });

                cn.FlushBuffer();

                sync.Set();

                await tcs.Task.ConfigureAwait(false);
            }, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            var requesterTask = Task.Factory.StartNew(async () =>
            {
                using var cn = cf.CreateConnection(opts);

                if(!sync.WaitOne(1000))
                    throw new Exception("Responder does not seem to be started.");

                for (var i = 0; i < 10; i++)
                    await cn.RequestAsync(subject, body, cts.Token).ConfigureAwait(false);

                cn.FlushBuffer();

                var sw = Stopwatch.StartNew();

                for (var i = 0; i < n; i++)
                    await cn.RequestAsync(subject, body, cts.Token).ConfigureAwait(false);

                sw.Stop();

                tcs.SetResult(sw.Elapsed);
            }, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            var elapsed = await tcs.Task.ConfigureAwait(false);

            cts.Cancel();

            await Task.WhenAll(responderTask, requesterTask).ConfigureAwait(false);

            return elapsed;
        }

        public static Task<TimeSpan> OldMyNatsClientRequestAsync(int n, ReadOnlyMemory<byte> payload, bool useTls)
        {
            Console.WriteLine($"MyNatsClient-Old-RequestAsync: n={n} s={payload.Length}");

            throw new NotImplementedException("You need to add reference to the NuGet package etc. to get this sample working.");

            //if(useTls)
            //    throw new ArgumentException("Old client does not support TLS.", nameof(useTls));

            //const string subject = "casea";
            //using var cts = new CancellationTokenSource();
            //using var sync = new AutoResetEvent(false);
            //var tcs = new TaskCompletionSource<TimeSpan>();
            //var cnInfo = new ConnectionInfo("127.0.0.1");

            //var body = payload.ToArray();

            //var responderTask = Task.Factory.StartNew(async () =>
            //{
            //    using var client = new NatsClient(cnInfo);
            //    client.Connect();

            //    client.Sub(subject, messages => messages.SubscribeSafe(msg => { client.Pub(msg.ReplyTo, msg.Payload); }));

            //    sync.Set();

            //    await tcs.Task.ConfigureAwait(false);
            //}, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            //var requesterTask = Task.Factory.StartNew(async () =>
            //{
            //    using var client = new NatsClient(cnInfo);

            //    client.Connect();

            //    if(!sync.WaitOne(1000))
            //        throw new Exception("Responder does not seem to be started.");

            //    for (var i = 0; i < 10; i++)
            //        await client.RequestAsync(subject, body).ConfigureAwait(false);

            //    var sw = Stopwatch.StartNew();

            //    for (var i = 0; i < n; i++)
            //        await client.RequestAsync(subject, body).ConfigureAwait(false);

            //    sw.Stop();
            //    tcs.SetResult(sw.Elapsed);
            //}, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            //var elapsed = await tcs.Task.ConfigureAwait(false);

            //cts.Cancel();

            //await Task.WhenAll(responderTask, requesterTask).ConfigureAwait(false);

            //return elapsed;
        }
    }
}
