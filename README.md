# MyNatsClient
My .Net client for NATS. Which is the result of me starting to looking into and learning NATS. You can read about it here in my blog series:

* [NATS, What a beautiful protocol](http://danielwertheim.se/nats-what-a-beautiful-protocol/)
* [Simple incoming OP parser for Nats in C#](http://danielwertheim.se/simple-incoming-op-parser-for-nats-in-c/)
* [Using the OpParser with Nats to create a long running consumer in C#](http://danielwertheim.se/using-the-opparser-with-nats-to-create-a-long-running-consumer-in-c/)
* [Continuing with C# and Nats, now looking at NatsObservable](http://danielwertheim.se/continuing-with-c-and-nats-now-looking-at-natsobservable/)
* [Time to construct a bundled NatsClient for C#](http://danielwertheim.se/time-to-construct-a-bundled-natsclient-for-csharp/)

The code is currently to be seen as a **labs project** (POC, proof of concept). But there's an offical client located here: https://github.com/nats-io/csnats

## Why a new one?
Because that's how I learn and I wanted to base mine around `IObservable<>` so that you could use ReactiveExtensions to consume incoming `Ops` from the server.

## Simple Consumer sample
Just some simple code showing usage.

```csharp
var connectionInfo = new ConnectionInfo(
    //Hosts to use. When connecting, will randomize the list
    //and try to connect. First successful will be used.
    new[]
    {
        new Host("192.168.1.176", 4222)
    })
{
    AutoRespondToPing = true,
    Verbose = true
};

//Client id (becomes part of subscription id)
using (var client = new NatsClient("myClientId", connectionInfo))
{
    //You can subscribe to dispatched client events
    //to react on something that happened to the client
    client.Events.OfType<ClientConnected>().Subscribe(ev =>
    {
        Console.WriteLine("Client connected!");
        ev.Client.Sub("foo", "s1");
        ev.Client.Sub("bar", "s2");

        //Make it automatically unsub after two messages
        ev.Client.UnSub("s2", 2);
    });
    
    client.Events.OfType<ClientFailed>().Subscribe(ev
        => Console.WriteLine($"Client failed with Exception: '{ev.Exception}'.");

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

    //Subscribe to IncomingOps All or e.g InfoOp, ErrorOp, MsgOp, PingOp, PongOp.
    client.IncomingOps.Subscribe(op =>
    {
        Console.WriteLine("===== RECEIVED =====");
        Console.Write(op.GetAsString());
        Console.WriteLine($"OpCount: {client.Stats.OpCount}");
    });

    client.IncomingOps.OfType<PingOp>().Subscribe(ping =>
    {
        if (!connectionInfo.AutoRespondToPing)
            client.Pong();
    });

    client.IncomingOps.OfType<MsgOp>().Subscribe(msg =>
    {
        Console.WriteLine("===== MSG =====");
        Console.WriteLine($"Subject: {msg.Subject}");
        Console.WriteLine($"QueueGroup: {msg.QueueGroup}");
        Console.WriteLine($"SubscriptionId: {msg.SubscriptionId}");
        Console.WriteLine($"Payload: {Encoding.UTF8.GetString(msg.Payload)}");

        //For demo purpose, force a fail
        if (Encoding.UTF8.GetString(msg.Payload) == "FAIL")
        {
            client.Send("FAIL");
        }
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
```
