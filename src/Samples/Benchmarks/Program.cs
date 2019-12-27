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
        private const int N = 50000;

        public static async Task Main(string[] args)
        {
            var testCase = args.FirstOrDefault();

            Task<TimeSpan> task;

            switch (testCase?.ToLowerInvariant())
            {
                case "a":
                    task = Scenarios.MyNatsClientRequestAsync(N, false);
                    break;
                case "asec":
                    task = Scenarios.MyNatsClientRequestAsync(N, true);
                    break;
                case "b":
                    task = Scenarios.NatsClientRequestAsync(N, false);
                    break;
                case "bsec":
                    task = Scenarios.NatsClientRequestAsync(N, true);
                    break;
                default:
                    Console.WriteLine("Select a scenario: [a,b]");
                    return;
            }

            var elapsed = await task.ConfigureAwait(false);

            Console.WriteLine($"Elapsed {elapsed}");
            Console.WriteLine($"{N / elapsed.TotalSeconds} msg/s");
        }
    }

    public static class Scenarios
    {
        public static async Task<TimeSpan> MyNatsClientRequestAsync(int n, bool useTls)
        {
            Console.WriteLine("MyNatsClient-RequestAsync");

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

                var body = new ReadOnlyMemory<byte>(new byte[32]);

                if(!sync.WaitOne(1000))
                    throw new Exception("Responder does not seem to be started.");

                for (var i = 0; i < 10; i++)
                    await client.RequestAsync(subject, body, cts.Token).ConfigureAwait(false);

                var sw = Stopwatch.StartNew();

                for (var i = 0; i < n; i++)
                    await client.RequestAsync(subject, body, cts.Token).ConfigureAwait(false);

                sw.Stop();
                tcs.SetResult(sw.Elapsed);
            }, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            var elapsed = await tcs.Task.ConfigureAwait(false);

            cts.Cancel();

            await Task.WhenAll(responderTask, requesterTask).ConfigureAwait(false);

            return elapsed;
        }

        public static async Task<TimeSpan> NatsClientRequestAsync(int n, bool useTls)
        {
            Console.WriteLine("NatsClient-RequestAsync");

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

                var body = new byte[32];

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
    }
}
