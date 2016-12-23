# MyNatsClient
## Why a new one when there's an official project?
Because I wanted one that leaves .Net4.0 behind and therefore offers `async` constructs. And I also wanted one that is reactive and supports [ReactiveExtensions](https://github.com/Reactive-Extensions/Rx.NET), by being based around `IObservable<T>`. And I also wanted to keep as much of the domain language of NATS as possible, but not strictly be following/limited to the APIs of other NATS client, but instead offer one that fits the .NET domain and one that is a .NET client first and foremost and not a port.

## Metrics
There are some posts on this on my blog. The most interesting one would be: [MyNatsClient - It flushes, but so can you](http://danielwertheim.se/mynatsclient-it-flushes-but-so-can-you/)

## DotNet Core
Yes it does support DotNet Core. From [v0.3.0-b1](https://www.nuget.org/packages/MyNatsClient/0.3.0-b1)

## License
Have fun using it ;-) [MIT](https://github.com/danielwertheim/mynatsclient/blob/master/LICENSE.txt)

## Install NuGet Package
If you just want the client and not the Reactive Extensions packages, use:

```
install-package MyNatsClient
```

For convenience, if you want a consumer that makes use of Reactive Extensions, use:

```
install-package MyNatsClient.Rx
```

## Simplest usage
Just some simple code showing usage.

```csharp
var connectionInfo = new ConnectionInfo("192.168.1.176")
{
    AutoReconnectOnFailure = true
};

using(var client = new NatsClient("clientId1", connectionInfo))
{
    //Pub-Sub sample
    await client.SubWithHandlerAsync("tick", msg => {
        Console.WriteLine($"Clock ticked. Tick is {msg.GetPayloadAsString()}");
    });

    await client.PubAsync("tick", getNextTick());

    //Request-Response sample
    await client.SubWithHandlerAsync("getTemp", msg => {
        client.Pub(msg.ReplyTo, getTemp(msg.GetPayloadAsString()));
    });

    var response = await client.RequestAsync("getTemp", "stockholm@sweden");
    Console.WriteLine($"Temp in Stockholm is {response.GetPayloadAsString()}");
}
```

## Advanced usage
Some code showing more advanced usage.

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
    AutoReconnectOnFailure = false,
    Verbose = false,
    Credentials = new Credentials("testuser", "p@ssword1234")
};

//The ClientId is not really used. Something you can use to look at
//if you have many clients running and same event handlers or something.
using (var client = new NatsClient("myClientId", connectionInfo))
{
    //You can subscribe to dispatched client events
    //to react on something that happened to the client
    client.Events.OfType<ClientConnected>().Subscribe(ev
        => Console.WriteLine("Client connected!"););
    
    client.Events.OfType<ClientConsumerFailed>().Subscribe(ev
        => Console.WriteLine($"Client consumer failed with Exception: '{ev.Exception}'.");

    //Disconnected, either by client.Disconnect() call
    //or caused by fail in your handlers.
    client.Events.OfType<ClientDisconnected>().Subscribe(ev =>
    {
        Console.WriteLine($"Client was disconnected due to reason '{ev.Reason}'");
        if (ev.Reason != DisconnectReason.DueToFailure)
            return;

        if(!connectionInfo.AutoReconnectOnFailure)
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
        Console.WriteLine($"ReplyTo: {msg.ReplyTo}");
        Console.WriteLine($"SubscriptionId: {msg.SubscriptionId}");
        Console.WriteLine($"Payload: {Encoding.UTF8.GetString(msg.Payload)}");
    });

    //Use the MsgOpStream, which ONLY will contain MsgOps, hence no filtering needed.
    client.MsgOpStream.Subscribe(msg =>
    {
        Console.WriteLine("===== MSG =====");
        Console.WriteLine($"Subject: {msg.Subject}");
        Console.WriteLine($"ReplyTo: {msg.ReplyTo}");
        Console.WriteLine($"SubscriptionId: {msg.SubscriptionId}");
        Console.WriteLine($"Payload: {Encoding.UTF8.GetString(msg.Payload)}");
    });

    var subscription = client.MsgOpStream.Subscribe(msg =>
    {
        //Will handle MsgOp matching Subject 'foo'
    }, msg => msg.Subject == "foo");

    client.Connect();

    Console.WriteLine("Hit key to UnSub from foo.");
    Console.ReadKey();

    //Either...
    client.UnSub(subscription.SubscriptionInfo);
    //Or...
    subscription.Dispose();

    Console.WriteLine("Hit key to Disconnect.");
    Console.ReadKey();
    client.Disconnect();
}
```

## Subscribing & Unsubscribing
The Client will keep track of subscriptions done. And you can set them up before connecting. Once it gets connected, it will register the subscriptions against the NATS server. If you make use of `ConnectionInfo.AutoReconnectOnFailure` it will also re-subscribe in the event of exceptions.

When subscribing to a subject using the client, you will be returned a `ISubscription`. The methods for subscribing are:

- `client.Sub(subscriptionInfo)`
- `client.SubAsync(subscriptionInfo)`
- `client.SubWithHandler(subscriptionInfo, msg => {})`
- `client.SubWithHandlerAsync(subscriptionInfo, msg => {})`
- `client.SubWithObserver(subscriptionInfo, observer)`
- `client.SubWithObserverAsync(subscriptionInfo, observer)`
- `client.SubWithObservableSubscription(subscriptionInfo, msgs => msgs.Subscribe(...))`
- `client.SubWithObservableSubscriptionAsync(subscriptionInfo, msgs => msgs.Subscribe(...))`

To `Unsubscribe`, you can do **any of the following**:

- Dispose the `ISubscription` returned by any of the subscribing methods listed above.
- Dispose the `NatsClient` and it will take care of the subscriptions.
- Pass the `ISubscription.SubscriptionInfo` to any of the `client.Unsub|UnsubAsync` methods
- Create the subscription using a `SubscriptionInfo` with `MaxMessages`, then it will auto unsubscribe after receiving the messages.

**NOTE** it's perfectly fine to do both e.g. `subscription.Dispose` as well as `consumer.Dispose` or e.g. `consumer.Unsubscribe` and then `subscription.Dispose`.

## Client.Events
The events aren't normal events, the events are distributed via `client.Events` which is an `IObservable<IClientEvent>`. The events are:

* ClientConnected
* ClientDisconnected
* ClientConsumerFailed

### ClientConnected
Signals that the client is connected and ready for use.

```csharp
client.Events.OfType<ClientConnected>().Subscribe(async ev => { });
```

### ClientDisconnected
You can use the `ClientDisconnected.Reason` to see if you manually should reconnect the client:

```csharp
client.Events.OfType<ClientDisconnected>().Subscribe(ev =>
{
    if (ev.Reason != DisconnectReason.DueToFailure)
        return;

    //Not needed if you use `ConnectionInfo.AutoReconnectOnFailure`.
    if(!connectionInfo.AutoReconnectOnFailure)
        ev.Client.Connect();
});
```

### ClientAutoReconnectFailed
If you use `ConnectionInfo.AutoReconnectOnFailure` and the client can not auto reconnect within a few attempts, this event will be raised.

```csharp
client.Events.OfType<ClientAutoReconnectFailed>().Subscribe(ev =>
{
    //Maybe manually try and connect again or something
    ev.Client.Connect();
});
```

### ClientConsumerFailed
This would be dispatched from the client, if the `Consumer` (internal part that continuously reads from server and dispatches messages) gets an `ErrOp` or if there's an `Exception`. E.g. if there's an unhandled exception from one of your **subscribed observers**.

## Connection behaviour
When creating the `ConnectionInfo` you can specify one or more `hosts`. It will try to get a connection to one of the servers. This is picked randomly and if no connection can be established to any of the hosts, an `NatsException` will be thrown.

## Auth
You specify credentials on the `ConnectionInfo` object or on individual hosts:

```csharp
var hosts = new [] {
    new Host("192.168.2.1"),
    new Host("192.168.2.2") {
        Credentials = new Credentials("foo", "bar")
    }
};
var cnInfo = new ConnectionInfo(hosts)
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

## SocketOptions
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

## Reads and Writes
The client uses one `Socket` but two `NetworkStreams`. One stream for writes and one for reads. The client only locks on writes.

## Synchronous and Asynchronous
The Client has both synchronous and asynchronous methods. They are pure versions and **NOT sync over async**. All async versions uses `ConfigureAwait(false)`.

## Observable message streams
The message streams are exposed as `Observables`. So you can use [ReactiveExtensions](https://github.com/Reactive-Extensions/Rx.NET) to consume e.g. the `client.OpStream` for `IOp` implementations: `ErrOp`, `InfoOp`, `MsgOp`, `PingOp`, `PongOp`. You do this using `client.OpStream.Subscribe(...)`. For `MsgOp`ONLY, use the `client.MsgOpStream.Subscribe(...)`.

For convenience you can install the `MyNatsClient.Rx` [NuGet package](http://www.nuget.org/packages/mynatsclient.rx), which then lets you do stuff like:

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
    Console.WriteLine($"ReplyTo: {msg.ReplyTo}");
    Console.WriteLine($"SubscriptionId: {msg.SubscriptionId}");
    Console.WriteLine($"Payload: {Encoding.UTF8.GetString(msg.Payload)}");
});

//Also proccess MsgOp explicitly via explicit MsgOpStream.
client.MsgOpStream.Subscribe(msg =>
{
    Console.WriteLine("===== MSG =====");
    Console.WriteLine($"Subject: {msg.Subject}");
    Console.WriteLine($"ReplyTo: {msg.ReplyTo}");
    Console.WriteLine($"SubscriptionId: {msg.SubscriptionId}");
    Console.WriteLine($"Payload: {Encoding.UTF8.GetString(msg.Payload)}");
});
```

### OpStream vs MsgOpStream
Why two, you confuse me? Well, in 99% of the cases you probably just care about `MsgOp`. Then instead of bothering about filtering etc. you just use the `MsgOpStream`. More efficient and simpler to use.

### Stateless
There's no buffering or anything going on with incoming `IOp` messages. So if you subscribe to a NATS subject using `client.Sub(...)`, but have no in-process subscription against `client.IncomingOps`, then those messages will just end up in getting discarded.

### InProcess Subscribtions vs NATS Subscriptions
The above is `in process subscribers` and you will not get any `IOp` dispatched to your handlers, unless you have told the client to subscribe to a NATS subject.

```csharp
client.Sub("subject", "subId");
//OR
await client.SubAsync("subject", "subId");
```

### Terminate an InProcess Subscription
The `client.IncomingOps.Subscribe(...)` returns an `IDisposable`. If you dispose that, your subscription to the observable is removed.

This will happen automatically if your subscription is causing an unhandled exception.

**PLEASE NOTE!** The NATS subscription is still there. Use `client.Unsub(...)` or `client.UnsubAsync(...)` to let the server know that your client should not receive messages for a certain subject anymore.

### Consumer pings and stuff
The Consumer looks at `client.Stats.LastOpReceivedAt` to see if it has taken to long time since it heard from the server.

**NOTE** this only kicks in as long as the client thinks the `Socket` is connected. If there's a known hard disconnect it will cleanly just get disconnected.

If `ConsumerPingAfterMsSilenceFromServer` (20000ms) has passed, it will start to `PING` the server.

If `ConsumerMaxMsSilenceFromServer` (60000ms) has passed, it will cause an exception and you will get notified via a `ClientConsumerFailed` event dispatched via `client.Events`. The Client will also be disconnected, and you will get the `ClientDisconnected` event, which you can use to reconnect.