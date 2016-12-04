# Release notes

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