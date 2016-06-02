# MyNatsClient
My .Net client for NATS. Which is the result of me starting to looking into and learning NATS. You can read about it here in my blog series:

* [NATS, What a beautiful protocol](http://danielwertheim.se/nats-what-a-beautiful-protocol/)
* [Simple incoming OP parser for Nats in C#](http://danielwertheim.se/simple-incoming-op-parser-for-nats-in-c/)
* [Using the OpParser with Nats to create a long running consumer in C#](http://danielwertheim.se/using-the-opparser-with-nats-to-create-a-long-running-consumer-in-c/)
* [Continuing with C# and Nats, now looking at NatsObservable](http://danielwertheim.se/continuing-with-c-and-nats-now-looking-at-natsobservable/)
* [Time to construct a bundled NatsClient for C#](http://danielwertheim.se/time-to-construct-a-bundled-natsclient-for-csharp/)

## Why a new one when there's an offical project?
Because I wanted to base mine around `IObservable<>` so that you could use ReactiveExtensions to consume incoming `Ops` from the server. And I also created this client as a way to learn about NATS itself.

For the official client, look here: https://github.com/nats-io/csnats

## .NET Core
MyNatsClient can, in its current shape, be compiled and distributed for .NET Core. MyNatsClient in itself does not have any dependencies on ReactiveExtensions. But your client will (if you want to use it). And to get that to work with a core project, you have to explicit import it as a dependency on a portable profile that RX currently supports.

The first releases will how-ever be distributed over NuGet for .NET 4.5 and soon .NET Core.

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
    Verbose = true,
    Credentials = new Credentials("testuser", "p@ssword1234")
};

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

## Auth
You specify credentials on the `ConnectionInfo` object:

```csharp
var cnInfo = new ConnectionInfo(...)
{
    Credentials = new Credentials("test", "p@ssword1234")
};
```

If the server is configured to require `user` and `pass`, you will get an exception if you have not provided credentials. It will look something like:

```
NatsException : No connection could be established against any of the specified servers.
```

With an inner exception of:

```
Error while connecting to ubuntu01:4223. Server requires credentials to be passed. None was specified.
```

## SocketFactory
If you like to tweak socket options, you inject your custom implementation of `ISocketFactory` on the client:

```csharp
client.SocketFactory = new MyMonoOptimizedSocketFactory();
```

## Logging
Some information is passed to a logger, e.g. Errors while trying to connect to a host. By default there's a `NullLogger` hooked in. To add a logger of choice, you would implement `ILogger` and assign a new resolver to `LoggerManager.Resolve`.

```csharp
public class MyLogger : ILogger {
    public void Debug(string message) {}
    public void Info(string message) {}
    public void Error(string message) {}
    public void Error(string message, Exception ex) {}
}
```

```csharp
LoggerManager.Resolve = loggerForType => new MyLogger();
```

The `loggerForType` being passed could be used for passing to NLog to get Logger per class etc.

## Client.Events
The events aren't normal events, the events are distributed via `client.Events` which is an `IObservable<IClientEvent>`. The events are:

* ClientConnected
* ClientDisconnected
* ClientConsumerFailed

### ClientConnected
Signals that the client is connected and ready for use. You can react on this to subscribe to `subjects`:

```csharp
client.Events.OfType<ClientConnected>().Subscribe(async ev =>
{
    await ev.Client.SubAsync("foo", "s1");
    await ev.Client.SubAsync("foo", "s2");
    await ev.Client.SubAsync("bar", "s3");

    //Make it automatically unsub after two messages
    await ev.Client.UnSubAsync("s2", 2);
});
```

### ClientDisconnected
You can use the `ClientDisconnected.Reason` to see if you manually should reconnect the client:

```csharp
client.Events.OfType<ClientDisconnected>().Subscribe(ev =>
{
    if (ev.Reason != DisconnectReason.DueToFailure)
        return;

    ev.Client.Connect();
});
```

### ClientConsumerFailed
This would be dispatched from the client, if the `Consumer` (internal part that continuously reads from server and dispatches messages) gets an `ErrOp` or if there's an `Exception`. E.g. if there's an unhandled exception from one of your subscribers.
