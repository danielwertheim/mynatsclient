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
                                var msg = $"Test{n}";
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
        }
    }
}
