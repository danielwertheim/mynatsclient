# Release notes

## v0.7.0 - 2016-12-23
Theme for this release has been "SIMPLIFY".

- **New**: All subscribe methods on the client now remembers subscriptions. So, if the client disconnects and reconnects, the subscriptions will get re-subscribed against the NATS server.
- **New**: All subscribe methods now returns an `ISubscription` which can be (not necessary) used to unsub, just by calling `subscription.Dispose()` or by calling `client.Unsub(subscription.SubscriptionInfo)`.
- **New**: Setup subscription to both NATS-server and in-process `IObservable<MsgOp>` stream in a single call using: `client.SubscribeWithHandler|SubscribeWithHandlerAsync` or `client.SubscribeWithObserver|SubscribeWithObserverAsync`.
- **New**: `client.RequestAsync` assists with implementing request-response messaging pattern.
- **Removed**: `NatsConsumer` as those features now are exposed by the `NatsClient` instead.
- **Removed**: Some overloads of `client.Sub` and `client.Unsub`.
- **Fixed**: Issue with not being able to do: `client.Connect()`, `client.Disconnect()` and `client.Connect()` really fast.
- **Fixed**: Issue with always trying to do auto reconnect upon failure.
- **Fixed**: Issue with missed `ConfigureAwait(false)`.

## v0.6.0 - 2016-12-04
- **New:** `ConnectionInfo.AutoReconnectOnFailure=true|false` default is `false`. Will try and reconnect if the client fails. If no reconnect can be made, an `ClientAutoReconnectFailed` will be dispatched.

## v0.5.1 - 2016-10-18
- **Fixed:** `NatsConsumer.Subscribe()` now support wildcard `"*"` and full wildcard `">"`.

## v0.5.0 - 2016-10-17
- **New:** `NatsConsumer.Subscribe()` now also accepts a handler with definition `Action<MsgOp>` and not only an `IObserver<MsgOp>`.
- **New:** The overloads of `NatsClient.Sub|SubAsync` that takes a `SubscriptionInfo` now makes an automatical call to `UnSub|UnSubAsync` if the `maxMessages` option has been specified.

## v0.4.0 - 2016-10-16
- **New:** Introducing `NatsConsumer` for simplified experience around message consumption. See README for more info.
- **Changed:** `client.UnSub` is now `client.Unsub` and `client.UnSubAsync` is now `client.Unsubasync`.

## v0.3.0 - 2016-10-08
- **New:** Support for DotNet Core.
- **New:** You can now have credentials on a specific host instead of common credentials. Specific overrides common.
- **Fixed:** `client.Pub` methods had `replyTo` as mandatory argument but it is optional.

## v0.2.1 - 2016-06-14
- **Closed:** [#7](https://github.com/danielwertheim/mynatsclient/issues/9), remove dependency on JSON.Net
- **Closed:** [#10](https://github.com/danielwertheim/mynatsclient/issues/9), provide MyNatsClient.Rx as a convenience NuGet for bringing in MyNatsClient and Rx.

## v0.1.2 - 2016-06-12
- **Fixed:** [#9](https://github.com/danielwertheim/mynatsclient/issues/9), with mix up of QueueGroup and ReplyTo

## v0.1.1 - 2016-06-12
- **Fixed:** Wrong dependencies in NuSpec

## v0.1.0 - 2016-06-12
First release.