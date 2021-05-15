using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using MyNatsClient;
using MyNatsClient.Rx;

namespace RequestResponseSample
{
    public class Program
    {
        private static INatsClient _client;

        public static async Task Main(string[] args)
        {
            LoggerManager.UseFactory(LoggerFactory.Create(b => b
                .AddFilter("System", LogLevel.Information)
                .AddFilter("Microsoft", LogLevel.Information)
                .SetMinimumLevel(LogLevel.Debug)
                .AddConsole()));

            var cnInfo = new ConnectionInfo("localhost");

            _client = new NatsClient(cnInfo);

            await _client.ConnectAsync();

            _client.Sub("getTemp", stream => stream.Subscribe(msg =>
            {
                var parts = msg.GetPayloadAsString().Split('@');
                _client.Pub(msg.ReplyTo, $"Temp is {TempService.Get(parts[0], parts[1])}C");
            }));

            while (true)
            {
                Console.WriteLine("Query? (y=yes;n=no)");
                if (Console.ReadKey().KeyChar == 'n')
                    break;

                Console.WriteLine();

                Console.WriteLine($"Got reply: {_client.RequestAsync("getTemp", "STOCKHOLM@SWEDEN").Result.GetPayloadAsString()}");
            }

            _client.Disconnect();
        }
    }

    internal static class TempService
    {
        private static readonly Random Rnd = new Random();

        internal static decimal Get(string city, string countryCode)
        {
            return Rnd.Next(-3000, 4200) / 100M;
        }
    }
}
