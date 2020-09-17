## Pub-Sub sample
Simple pub-sub sample showing one client that publishes and one that subscribes. This can of course be the same client and you can also have more clients subscribing etc.

**Publisher**

```csharp
var cnInfo = new ConnectionInfo("192.168.1.10");
var client = new NatsClient(cnInfo);

await client.ConnectAsync();

await client.PubAsync("tick", GetNextTick());

//or using an encoding package e.g. Json
await client.PubAsJsonAsync("tickItem", new Tick { Value = GetNextTick() });
```

**Subscriber**

```csharp
var cnInfo = new ConnectionInfo("192.168.1.10");
var client = new NatsClient(cnInfo);

await _client.ConnectAsync();

await client.SubAsync("tick", stream => stream.Subscribe(msg => {
    Console.WriteLine($"Clock ticked. Tick is {msg.GetPayloadAsString()}");
}));

//or using an encoding package e.g Json
await client.SubAsync("tickItem", stream => stream.Subscribe(msg => {
    Console.WriteLine($"Clock ticked. Tick is {msg.FromJson<TestItem>().Value}");
}))
```