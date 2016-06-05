# MyNatsClient
My .Net client for NATS. Which is the result of me starting to looking into and learning NATS. You can read about it here in my blog series:

* [NATS, What a beautiful protocol](http://danielwertheim.se/nats-what-a-beautiful-protocol/)
* [Simple incoming OP parser for Nats in C#](http://danielwertheim.se/simple-incoming-op-parser-for-nats-in-c/)
* [Using the OpParser with Nats to create a long running consumer in C#](http://danielwertheim.se/using-the-opparser-with-nats-to-create-a-long-running-consumer-in-c/)
* [Continuing with C# and Nats, now looking at NatsObservable](http://danielwertheim.se/continuing-with-c-and-nats-now-looking-at-natsobservable/)
* [Time to construct a bundled NatsClient for C#](http://danielwertheim.se/time-to-construct-a-bundled-natsclient-for-csharp/)

# Why a new one when there's an offical project?
Because I wanted to base mine around `IObservable<>` so that you could use [ReactiveExtensions](https://github.com/Reactive-Extensions/Rx.NET) to consume incoming `Ops` from the server.

And I also wanted to keep as much of the domain language of NATS but not necesarily follow APIs of other NATS client, but instead offer one that fits the .NET domain.

Finally, I also created this client as a way to learn about NATS itself.

For the official client, look here: https://github.com/nats-io/csnats

# .NET Core
MyNatsClient can, in its current shape, be compiled and distributed for .NET Core. MyNatsClient in itself does not have any dependencies on [ReactiveExtensions](https://github.com/Reactive-Extensions/Rx.NET). But your client will (if you want to use it). And to get that to work with a core project, you have to explicit import it as a dependency on a portable profile that RX currently supports.

The first releases will how-ever be distributed over NuGet for .NET 4.5 and soon .NET Core.

# License
Have fun using it ;-) [MIT](https://github.com/danielwertheim/mynatsclient/blob/master/LICENSE.txt)

# Issues, Questions etc.
Found any Issues? Cool, then someone is using it. Just report them under Issues.

Have any questions? Awesome. Ping me on Twitter @danielwertheim.

# Consumer sample
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
    Verbose = false,
    Credentials = new Credentials("testuser", "p@ssword1234")
};

//The ClientId is not really used. Something you can use to look at
//if you have many clients running and same event handlers or something.
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

    //Subscribe to OpStream to get ALL ops e.g InfoOp, ErrorOp, MsgOp, PingOp, PongOp.
    client.OpStream.Subscribe(op =>
    {
        Console.WriteLine("===== RECEIVED =====");
        Console.Write(op.GetAsString());
        Console.WriteLine($"OpCount: {client.Stats.OpCount}");
    });

    //Filter for specific types
    client.OpStream.OfType<PingOp>().Subscribe(ping =>
    {
        if (!connectionInfo.AutoRespondToPing)
            client.Pong();
    });

    client.OpStream.OfType<MsgOp>().Subscribe(msg =>
    {
        Console.WriteLine("===== MSG =====");
        Console.WriteLine($"Subject: {msg.Subject}");
        Console.WriteLine($"QueueGroup: {msg.QueueGroup}");
        Console.WriteLine($"SubscriptionId: {msg.SubscriptionId}");
        Console.WriteLine($"Payload: {Encoding.UTF8.GetString(msg.Payload)}");
    });

    //Use the MsgOpStream, which ONLY will contain MsgOps, hence no filtering needed.
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
```

# Auth
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

# SocketOptions
You can adjust the `SocketOptions` by configuring the following:

```csharp
public class SocketOptions
{
    /// <summary>
    /// Gets or sets the ReceiveBufferSize of the Socket.
    /// Will also adjust the buffer size of the underlying <see cref="System.IO.BufferedStream"/>
    /// that is used by the consumer.
    /// </summary>
    public int? ReceiveBufferSize { get; set; }

    /// <summary>
    /// Gets or sets the SendBufferSize of the Socket.
    /// Will also adjust the buffer size of the underlying <see cref="System.IO.BufferedStream"/>
    /// that is used by the publisher.
    /// </summary>
    public int? SendBufferSize { get; set; }

    /// <summary>
    /// Gets or sets the ReceiveTimeoutMs for the Socket.
    /// When it times out, the client will look at internal settings
    /// to determine if it should fail or first try and ping the server.
    /// </summary>
    public int? ReceiveTimeoutMs { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the SendTimeoutMs for the Socket.
    /// </summary>
    public int? SendTimeoutMs { get; set; }
}
```

# SocketFactory
If you like to tweak socket options, you inject your custom implementation of `ISocketFactory` on the client:

```csharp
client.SocketFactory = new MyMonoOptimizedSocketFactory();
```

# Logging
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

# Client.Events
The events aren't normal events, the events are distributed via `client.Events` which is an `IObservable<IClientEvent>`. The events are:

* ClientConnected
* ClientDisconnected
* ClientConsumerFailed

## ClientConnected
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

## ClientDisconnected
You can use the `ClientDisconnected.Reason` to see if you manually should reconnect the client:

```csharp
client.Events.OfType<ClientDisconnected>().Subscribe(ev =>
{
    if (ev.Reason != DisconnectReason.DueToFailure)
        return;

    ev.Client.Connect();
});
```

## ClientConsumerFailed
This would be dispatched from the client, if the `Consumer` (internal part that continuously reads from server and dispatches messages) gets an `ErrOp` or if there's an `Exception`. E.g. if there's an unhandled exception from one of your subscribers.

# Connection behaviour
When creating the `ConnectionInfo` you can specify one or more `hosts`. It will try to get a connection to one of the servers. This is picked randomly and if no connection can be established to any of the hosts, an `NatsException` will be thrown.

# Reads and Writes
The client uses one `Socket` but two `NetworkStreams`. One stream for writes and one for reads. The client only locks on writes.

# Synchronous and Asynchronous
The Client has both synchronous and asynchronous methods. They are pure versions and **NOT sync over async**. All async versions uses `ConfigureAwait(false)`.

```csharp
void Ping();
Task PingAsync();

void Pong();
Task PongAsync();

void Pub(string subject, string body, string replyTo);
void Pub(string subject, byte[] body, string replyTo);
Task PubAsync(string subject, string body, string replyTo);
Task PubAsync(string subject, byte[] body, string replyTo);

void Sub(string subject, string subscriptionId, string queueGroup = null);
Task SubAsync(string subject, string subscriptionId, string queueGroup = null);

void UnSub(string subscriptionId, int? maxMessages = null);
Task UnSubAsync(string subscriptionId, int? maxMessages = null);
```

# Consuming
The Consumer is the part that consumes the readstream. It tries to parse the incoming data to `IOp` implementations: `ErrOp`, `InfoOp`, `MsgOp`, `PingOp`, `PongOp`; which you consume via `client.OpStream.Subscribe(...)` or for `MsgOp`ONLY, use the `client.MsgOpStream.Subscribe(...)`. The Sample client is using [ReactiveExtensions](https://github.com/Reactive-Extensions/Rx.NET) and with this in place, you can do stuff like:

```csharp
//Subscribe to OpStream ALL ops e.g InfoOp, ErrorOp, MsgOp, PingOp, PongOp.
client.OpStream.Subscribe(op =>
{
    Console.WriteLine("===== RECEIVED =====");
    Console.Write(op.GetAsString());
    Console.WriteLine($"OpCount: {client.Stats.OpCount}");
});

//Also proccess PingOp explicitly
client.OpStream.OfType<PingOp>().Subscribe(ping =>
{
    if (!connectionInfo.AutoRespondToPing)
        client.Pong();
});

//Also proccess MsgOp explicitly via filter on ALL OpStream
client.OpStream.OfType<MsgOp>().Subscribe(msg =>
{
    Console.WriteLine("===== MSG =====");
    Console.WriteLine($"Subject: {msg.Subject}");
    Console.WriteLine($"QueueGroup: {msg.QueueGroup}");
    Console.WriteLine($"SubscriptionId: {msg.SubscriptionId}");
    Console.WriteLine($"Payload: {Encoding.UTF8.GetString(msg.Payload)}");
});

//Also proccess MsgOp explicitly via explicit MsgOpStream.
client.MsgOpStream.Subscribe(msg =>
{
    Console.WriteLine("===== MSG =====");
    Console.WriteLine($"Subject: {msg.Subject}");
    Console.WriteLine($"QueueGroup: {msg.QueueGroup}");
    Console.WriteLine($"SubscriptionId: {msg.SubscriptionId}");
    Console.WriteLine($"Payload: {Encoding.UTF8.GetString(msg.Payload)}");
});
```

## OpStream vs MsgOpStream
Why two, you confuse me? Well, in 99% of the cases you probably just care about `MsgOp`. Then instead of bothering about filtering etc. you just use the `MsgOpStream`. More efficient and simpler to use.

## Stateless
There's no buffering or anything going on with incoming `IOp` messages. So if you subscribe to a NATS subject using `client.Sub(...)`, but have no in-process subscription against `client.IncomingOps`, then those messages will just end up in getting discarded.

## InProcess Subscribtions vs NATS Subscriptions
The above is `in process subscribers` and you will not get any `IOp` dispatched to your handlers, unless you have told the client to subscribe to a NATS subject.

```csharp
client.Sub("subject", "subId");
//OR
await client.SubAsync("subject", "subId");
```

## Terminate an InProcess Subscription
The `client.IncomingOps.Subscribe(...)` returns an `IDisposable`. If you dispose that, your subscription to the observable is removed.

This will happen automatically if your subscription is causing an unhandled exception.

**PLEASE NOTE!** The NATS subscription is still there. Use `client.UnSub(...)` or `client.UnSubAsync(...)` to let the server know that your client should not receive messages for a certain subject anymore.

## Consumer pings and stuff
The Consumer looks at `client.Stats.LastOpReceivedAt` to see if it has taken to long time since it heard from the server.

**NOTE** this only kicks in as long as the client thinks the `Socket` is connected. If there's a known hard disconnect it will cleanly just get disconnected.

If `ConsumerPingAfterMsSilenceFromServer` (20000ms) has passed, it will start to `PING` the server.

If `ConsumerMaxMsSilenceFromServer` (60000ms) has passed, it will cause an exception and you will get notified via a `ClientConsumerFailed` event dispatched via `client.Events`. The Client will also be disconnected, and you will get the `ClientDisconnected` event, which you can use to reconnect.

# Timings
The code for this is included in the Samples in this repo.

The measurements are reported `Sender` vs `Consumer`, but these are running in parallel.

One sender console publishes 100000 messages (one per call), 10 times. Different payloads are used. It reports how long time it took to dispatch all 100000 messages.

One consumer console consumes these messages, and reports how long time it spent consuming 100000 messages.

## Environment
Everything is running on a physical Windosw10 64bit, 32GB RAM i7-4790K quad core computer.

The Sender console and Consumer console is running as separate processes on this machine.

The NATS server is running on the same machine, in a Docker container, on a Ubuntu 14.04.4 LTS, with 8GB RAM, 2 virtual cores, via Hyper-V.

### Publisher timings
Timed `2016-06-05` which is `pre v1.0.0`.

```
===== RESULT =====
BatchSize: 100000
BodySize: 10
228ms
240ms
232ms
252ms
228ms
234ms
286ms
242ms
274ms
240ms
Avg ms per batch	245,6
Avg ms per mess	0,002456
Avg ms per byte	0,0002456
Avg ms per mb	245,6
Avg ms per batch (excluding lowest & highest)	247,555555555556
Avg ms per mess (excluding lowest & highest)	0,00247555555555556
Avg ms per byte (excluding lowest & highest)	0,000247555555555556
Avg ms per mb (excluding lowest & highest)	247,555555555556
```

```
===== RESULT =====
BatchSize: 100000
BodySize: 100
389ms
488ms
584ms
406ms
402ms
417ms
403ms
399ms
497ms
410ms
Avg ms per batch	439,5
Avg ms per mess	0,004395
Avg ms per byte	4,395E-05
Avg ms per mb	43,95
Avg ms per batch (excluding lowest & highest)	445,111111111111
Avg ms per mess (excluding lowest & highest)	0,00445111111111111
Avg ms per byte (excluding lowest & highest)	4,45111111111111E-05
Avg ms per mb (excluding lowest & highest)	44,5111111111111
```

```
===== RESULT =====
BatchSize: 100000
BodySize: 1000
998ms
57ms
294ms
17ms
57ms
93ms
272ms
46ms
1ms
23ms
Avg ms per batch	185,8
Avg ms per mess	0,001858
Avg ms per byte	1,858E-06
Avg ms per mb	1,858
Avg ms per batch (excluding lowest & highest)	206,333333333333
Avg ms per mess (excluding lowest & highest)	0,00206333333333333
Avg ms per byte (excluding lowest & highest)	2,06333333333333E-06
Avg ms per mb (excluding lowest & highest)	2,06333333333333
```

```
===== RESULT =====
BatchSize: 100000
BodySize: 10000
344ms
470ms
773ms
446ms
584ms
369ms
475ms
553ms
461ms
364ms
Avg ms per batch	483,9
Avg ms per mess	0,004839
Avg ms per byte	4,839E-07
Avg ms per mb	0,4839
Avg ms per batch (excluding lowest & highest)	499,444444444444
Avg ms per mess (excluding lowest & highest)	0,00499444444444444
Avg ms per byte (excluding lowest & highest)	4,99444444444444E-07
Avg ms per mb (excluding lowest & highest)	0,499444444444444
```

### Consumer timings
Timed `2016-06-05` which is `pre v1.0.0`.

```
===== RESULT =====
BatchSize: 100000
BodySize: 10
262,9573ms
240,2051ms
232,8512ms
250,5377ms
228,0289ms
234,6012ms
290,346ms
242,2346ms
273,6868ms
239,9918ms
Avg ms per batch	249,54406
Avg ms per mess	0,0024954406
Avg ms per byte	0,00024954406
Avg ms per mb	249,54406
Avg ms per batch (excluding lowest & highest)	251,934633333333
Avg ms per mess (excluding lowest & highest)	0,00251934633333333
Avg ms per byte (excluding lowest & highest)	0,000251934633333333
Avg ms per mb (excluding lowest & highest)	251,934633333333
```

```
===== RESULT =====
BatchSize: 100000
BodySize: 100
384,0295ms
488,4619ms
584,221ms
406,3696ms
402,2874ms
417,3684ms
402,5042ms
399,0836ms
497,1408ms
411,0902ms
Avg ms per batch	439,25566
Avg ms per mess	0,0043925566
Avg ms per byte	4,3925566E-05
Avg ms per mb	43,925566
Avg ms per batch (excluding lowest & highest)	445,3919
Avg ms per mess (excluding lowest & highest)	0,004453919
Avg ms per byte (excluding lowest & highest)	4,453919E-05
Avg ms per mb (excluding lowest & highest)	44,53919
```

```
===== RESULT =====
BatchSize: 100000
BodySize: 1000
1994,3751ms
2056,7058ms
2293,8253ms
2013,0667ms
2057,2065ms
2112,0543ms
2264,694ms
2026,4929ms
1995,7137ms
2017,5575ms
Avg ms per batch	2083,16918
Avg ms per mess	0,0208316918
Avg ms per byte	2,08316918E-05
Avg ms per mb	20,8316918
Avg ms per batch (excluding lowest & highest)	2093,03518888889
Avg ms per mess (excluding lowest & highest)	0,0209303518888889
Avg ms per byte (excluding lowest & highest)	2,09303518888889E-05
Avg ms per mb (excluding lowest & highest)	20,9303518888889
```

```
===== RESULT =====
BatchSize: 100000
BodySize: 10000
17341,213ms
17466,481ms
17772,0726ms
17427,3979ms
17571,934ms
17351,7579ms
17464,0851ms
17551,7528ms
17452,9309ms
17347,1624ms
Avg ms per batch	17474,67876
Avg ms per mess	0,1747467876
Avg ms per byte	1,747467876E-05
Avg ms per mb	17,47467876
Avg ms per batch (excluding lowest & highest)	17489,5082888889
Avg ms per mess (excluding lowest & highest)	0,174895082888889
Avg ms per byte (excluding lowest & highest)	1,74895082888889E-05
Avg ms per mb (excluding lowest & highest)	17,4895082888889
```