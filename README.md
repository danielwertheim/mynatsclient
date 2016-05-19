# MyNatsClient
My .Net client for NATS. Which is the result of me starting to looking into and learning NATS. You can read about it here in my blog series:

* [NATS, What a beautiful protocol](http://danielwertheim.se/nats-what-a-beautiful-protocol/)
* [Simple incoming OP parser for Nats in C#](http://danielwertheim.se/simple-incoming-op-parser-for-nats-in-c/)
* [Using the OpParser with Nats to create a long running consumer in C#](http://danielwertheim.se/using-the-opparser-with-nats-to-create-a-long-running-consumer-in-c/)
* [Continuing with C# and Nats, now looking at NatsObservable](http://danielwertheim.se/continuing-with-c-and-nats-now-looking-at-natsobservable/)
* [Time to construct a bundled NatsClient for C#](http://danielwertheim.se/time-to-construct-a-bundled-natsclient-for-csharp/)

The code is currently to be seen as a **labs project**. But there's an offical client located here: https://github.com/nats-io/csnats

## Why a new one?
Because that's how I learn and I wanted to base mine around `IObservable<>` so that you could use ReactiveExtensions to consume incoming `Ops` from the server.
