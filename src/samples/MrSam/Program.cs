using System;
using System.Reactive;
using MyNatsClient;
using MyNatsClient.Ops;

namespace MrSam
{
    public class Program
    {
        private static INatsClient _client;

        public static void Main(string[] args)
        {
            var cnInfo = new ConnectionInfo("192.168.2.17")
            {
                Credentials = new Credentials("test", "p@ssword123")
            };
            _client = new NatsClient("myclient", cnInfo);


            var c = 0;

            //var sub = _client.SubWithObserver("test", new DelegatingObserver<MsgOp>(
            //    msg =>
            //    {
            //        Console.WriteLine($"Observer OnNext got: {msg.GetPayloadAsString()}");

            //        throw new Exception(c++.ToString());
            //    },
            //    err =>
            //    {
            //        Console.WriteLine("Observer OnError got:" + err.Message);
            //    },
            //    () =>
            //    {
            //        Console.WriteLine("Observer completed");
            //    }));

            var sub = _client.Sub("test");
            _client.OpStream.OnException = (op, ex) =>
            {
                Console.WriteLine("Generic op err...");
            };
            _client.MsgOpStream.OnException = (msg, ex) =>
            {
                Console.WriteLine("Generic msg err...");
            };
            _client.MsgOpStream.Subscribe(new DelegatingObserver<MsgOp>(
                msg =>
                {
                    Console.WriteLine($"Observer OnNext got: {msg.GetPayloadAsString()}");

                    throw new Exception(c++.ToString());
                },
                err =>
                {
                    Console.WriteLine("Observer OnError got:" + err.Message);
                },
                () =>
                {
                    Console.WriteLine("Observer completed");
                }));

            //_client.MsgOpStream.Subscribe(msg =>
            //{
            //    Console.WriteLine($"Observer OnNext got: {msg.GetPayloadAsString()}");

            //    throw new Exception(c++.ToString());
            //});

            _client.Connect();

            while (true)
            {
                Console.WriteLine("Run? (y=yes;n=no)");
                var key = Console.ReadKey().KeyChar;
                Console.WriteLine();
                if (key == 'n')
                    break;

                _client.Pub("test", $"test{c.ToString()}");
            }

            sub.Dispose();

            _client.Disconnect();

            Console.ReadKey();

            //_client.SubWithHandler("getTemp", msg =>
            //{
            //    var parts = msg.GetPayloadAsString().Split('@');
            //    _client.Pub(msg.ReplyTo, $"Temp is {TempService.Get(parts[0], parts[1])}C");
            //});

            ////int c = 0;

            //while (true)
            //{
            //    Console.WriteLine("Query? (y=yes;n=no)");
            //    if (Console.ReadKey().KeyChar == 'n')
            //        break;

            //    Console.WriteLine();

            //    //c++;
            //    Console.WriteLine($"Got reply: {_client.RequestAsync("getTemp", "STOCKHOLM@SWEDEN").Result.GetPayloadAsString()}");
            //    //if (c % 5 == 0)
            //    //{
            //    //    _client.Disconnect();
            //    //    _client.Connect();
            //    //}
            //}

            //_client.Disconnect();
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
