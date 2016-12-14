using System;
using System.Reactive.Linq;
using MyNatsClient;

namespace MrSam
{
    public class Program
    {
        private static INatsClient _client;

        public static void Main(string[] args)
        {
            var cnInfo = new ConnectionInfo(new Host("192.168.2.20"));
            _client = new NatsClient("testid", cnInfo);
            _client.Connect();

            _client.MsgOpStream.Where(msg => msg.Subject == "getTemp").Subscribe(msg =>
            {
                var parts = msg.GetPayloadAsString().Split('@');
                _client.Pub(msg.ReplyTo, $"Temp is {TempService.Get(parts[0], parts[1])}C");
            });
            _client.Sub("getTemp", "s2");

            using (var request = new NatsRequester(_client))
            {
                while (true)
                {
                    Console.WriteLine("Query? (y=yes;n=no)");
                    if (Console.ReadKey().KeyChar == 'n')
                        break;

                    Console.WriteLine();

                    Console.WriteLine($"Got reply: {request.RequestAsync("getTemp", "STOCKHOLM@SWEDEN").Result.GetPayloadAsString()}");
                }
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
