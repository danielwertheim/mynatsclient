# MyNatsClient
A **.NETStandard2.0 and .NET 4.5.1+**, `async` and [ReactiveExtensions](https://github.com/Reactive-Extensions/Rx.NET) (RX) friendly client for [NATS Server](https://nats.io). It's RX friendly cause it's based around `IObservable<T>`. It keeps as much of NATS domain language as possible but does not limit itself to follow the APIs of other NATS clients, but instead offer one that fits the .NET domain and one that first and foremost is a client written for .NET. Not GO or JAVA or Foo.

It offers both simple and advanced usage. By default it's configured to auto reply on heartbeat pings and to reconnect on failures. You can seed it with multiple hosts in a cluster. So if one fails it will reconnect to another one.

If one of your in-process observer subscription fails, it will not unsubscribe them. It will however invoke the observers `OnError` callback. This is done so that one failing message against a certain subject does not prevent future messages from being handled.

It keeps track of when the last contact to a server was, so that it can send a `PING` to see if server is still alive.

Instead of relying on background flushing like other clients do, it **auto flushes** for each `PUB`. You can also use the construct `client.PubMany` to publish many messages and get one flush for them all. Finally **you can also control the flushing manually**.

It supports:

- Pub-Sub
- Request-Response
- Queue groups

## Samples
There's a separate repo for [the samples](https://github.com/danielwertheim/mynatsclient-samples)

## .NET and .NET Standard
The project multi targets the following frameworks:

- .NET4.5.1
- .NETStandard2.0

## Metrics
Fast? Yes it is. More info can be found here: [MyNatsClient - It flushes, but so can you](http://danielwertheim.se/mynatsclient-it-flushes-but-so-can-you/)

## License
MyNatsClient is licensed under [MIT](https://github.com/danielwertheim/mynatsclient/blob/master/LICENSE.txt) so have fun using it.

## Release notes
They are kept separately [here](https://github.com/danielwertheim/mynatsclient/blob/master/ReleaseNotes.md).

## Install NuGet Package
If you just want the client and not the Reactive Extensions packages, use:

```
install-package MyNatsClient
```

For convenience, if you want a consumer that makes use of Reactive Extensions, use:

```
install-package MyNatsClient.Rx
```

### Encodings
You can also get simplified support for specific payload encodings.

```
install-package MyNatsClient.Encodings.Json
```

This gives you a `JsonEncoding` and some pre-made extension methods under `MyNatsClient.Encodings.Json.Extensions`

## Pub-Sub sample
Simple pub-sub sample showing one client that publishes and one that subscribes. This can of course be the same client and you can also have more clients subscribing etc.

**Publisher**

```csharp
var cnInfo1 = new ConnectionInfo("192.168.1.10");
var client1 = new NatsClient(cnInfo);

await client1.PubAsync("tick", GetNextTick());

//or using an encoding package e.g. Json
await client1.PubAsJsonAsync("tickItem", new Tick { Value = GetNextTick() });
```

**Subscriber**

```csharp
var cnInfo2 = new ConnectionInfo("192.168.1.20");
var client2 = new NatsClient(cnInfo);

await client2.SubAsync("tick", msg => {
    Console.WriteLine($"Clock ticked. Tick is {msg.GetPayloadAsString()}");
});

//or using an encoding package e.g Json
await client2.Subsync("tickItem", msg => {
    Console.WriteLine($"Clock ticked. Tick is {msg.FromJson<TestItem>().Value}");
})
```

## Request-Response sample
Simple request-response sample. This sample also makes use of two clients. It can of course be the same client requesting and responding, you can also have more responders forming a queue group. Where one will be giving the answer.

**Requester**

```csharp
var cnInfo1 = new ConnectionInfo("192.168.1.10");
var client1 = new NatsClient(cnInfo);

var response = await client1.RequestAsync("getTemp", "stockholm@sweden");
Console.WriteLine($"Temp in Stockholm is {response.GetPayloadAsString()}");
```

**Responder**

```csharp
var cnInfo2 = new ConnectionInfo("192.168.1.20");
var client2 = new NatsClient(cnInfo);

await client2.SubAsync("getTemp", msg => {
    client.Pub(msg.ReplyTo, getTemp(msg.GetPayloadAsString()));
});
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
    AutoReconnectOnFailure = true,
    Verbose = false,
    Credentials = new Credentials("testuser", "p@ssword1234"),
    RequestTimeoutMs = 5000,
    PubFlushMode = PubFlushMode.Auto,
    SocketOptions = new SocketOptions
    {
        ReceiveTimeoutMs = 5000,
        SendTimeoutMs = 5000,
        ConnectTimeoutMs = 5000,
        ReceiveBufferSize = null, //.NET & OS default
        SendBufferSize = null //.NET & OS default
    }
};

using (var client = new NatsClient(connectionInfo))
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

    var subscription = client.Sub("foo");

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
- `client.Sub(subscriptionInfo, msg => {})`
- `client.Sub(subscriptionInfo, observer)`
- `client.Sub(subscriptionInfo, msgs => msgs.Subscribe(...))`
- `client.SubAsync(subscriptionInfo)`
- `client.SubAsync(subscriptionInfo, msg => {})`
- `client.SubAsync(subscriptionInfo, observer)`
- `client.SubAsync(subscriptionInfo, msgs => msgs.Subscribe(...))`

To `Unsubscribe`, you can do **any of the following**:

- Dispose the `ISubscription` returned by any of the subscribing methods listed above.
- Dispose the `NatsClient` and it will take care of the subscriptions.
- Pass the `ISubscription` or the `SubscriptionInfo` to any of the `client.Unsub|UnsubAsync` methods
- Create the subscription using a `SubscriptionInfo` with `MaxMessages`, then it will auto unsubscribe after receiving the messages.

**NOTE** it's perfectly fine to do both e.g. `subscription.Dispose` as well as `consumer.Dispose` or e.g. `consumer.Unsubscribe` and then `subscription.Dispose`.

## Client.Events
The events aren't normal events, the events are distributed via `client.Events` which is an `IObservable<IClientEvent>`. The events are:

* ClientConnected
* ClientDisconnected
* ClientAutoReconnectFailed
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
    /// Gets or sets the Recieve timeout in milliseconds for the Socket.
    /// When it times out, the client will look at internal settings
    /// to determine if it should fail or first try and ping the server.
    /// </summary>
    public int? ReceiveTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the Send timeout in milliseconds for the Socket.
    /// </summary>
    public int? SendTimeoutMs { get; set; } = 5000;


    /// <summary>
    /// Gets or sets the Connect timeout in milliseconds for the Socket.
    /// </summary>
    public int ConnectTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets value indicating if the Nagle algoritm should be used or not
    /// on the created Socket.
    /// </summary>
    public bool? UseNagleAlgorithm { get; set; } = false;
}
```

## SocketFactory
If you like to tweak socket options, you inject your custom implementation of `ISocketFactory` to the client:

```csharp
var client = new NatsClient(cnInfo, new MyMonoOptimizedSocketFactory());
```

## Logging
Some information is passed to a logger, e.g. Errors while trying to connect to a host. By default there's a `NullLogger` hooked in. To add a logger of choice, you would implement `ILogger` and assign a new resolver to `LoggerManager.Resolve`.

```csharp
public class MyLogger : ILogger {
    public void void Trace(string message) {}
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

## Consumer pings and stuff
The Consumer keeps track of how long it was since it got a message from the broker to see if it has taken to long time since it heard from it.

**NOTE** this only kicks in as long as the client thinks the `Socket` is connected. If there's a known hard disconnect it will cleanly just get disconnected.

If `ConsumerPingAfterMsSilenceFromServer` (20000ms) has passed, it will start to `PING` the server.

If `ConsumerMaxMsSilenceFromServer` (40000ms) has passed, it will cause an exception and you will get notified via a `ClientConsumerFailed` event dispatched via `client.Events`. The Client will also be disconnected, and you will get the `ClientDisconnected` event, which you can use to reconnect.

## Exceptions
### Catch all unhandled exceptions per stream
The Client exposes e.g. `MsgOpStream` which has a `OnException` delegate defined. You can use it to get notified about exceptions. These exceptions will automatically be logged via `ILogger.Error` which is resolved via `LoggerManager.Resolve` which is something you can hook into.

### Catch exceptions using the AnonymousObserver
Subscribing with the use of an observer makes it easy for you to catch exceptions and handle them.

```csharp
var c = 0;

//Only the OnNext (the first argument) is required.
var myObserver = new AnonymousObserver<MsgOp>(
  msg =>
  {
    Console.WriteLine($"Observer OnNext got: {msg.GetPayloadAsString()}");

    throw new Exception(c++.ToString());
  },
  err =>
    Console.WriteLine("Observer OnError got:" + err.Message),
  () =>
    Console.WriteLine("Observer completed"));

//Subscribe to subject "test" and hook up the observer
//for incoming messages on that subject
var sub = _client.Sub("test", myObserver);


//Publish some messages
while (true)
{
  Console.WriteLine("Run? (y=yes;n=no)");
  var key = Console.ReadKey().KeyChar;

  Console.WriteLine();
  if (key == 'n')
    break;
  
  _client.Pub("test", $"test{c.ToString()}");
}

//Tear down subscription (both against NATS server and observable stream)
sub.Dispose();
```

This will give the following output:

```bash
Run? (y=yes;n=no)
y
Run? (y=yes;n=no)
Observer OnNext got: test0
Observer OnError got:0
y
Run? (y=yes;n=no)
Observer OnNext got: test1
Observer OnError got:1
n
Observer completed
```

### Exceptions and "handlers"
The handler will just swallow the exception and continue working.

Changing the subscribing part from the first sample above to:

```csharp
var sub = _client.Sub("test", msg =>
{
  Console.WriteLine($"Observer OnNext got: {msg.GetPayloadAsString()}");

  throw new Exception(c++.ToString());
});
```

This will give the following output:

```bash
Run? (y=yes;n=no)
y
Run? (y=yes;n=no)
Observer OnNext got: test0
y
Run? (y=yes;n=no)
Observer OnNext got: test1
y
Run? (y=yes;n=no)
Observer OnNext got: test2
y
Run? (y=yes;n=no)
Observer OnNext got: test3
n
```
