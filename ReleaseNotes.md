# Release notes

## v0.12.0 - UPCOMING

- **Changed**: Multi targeting frameworks: `NETStandard2.0`, `.NET4.5.1`.
- **Changed**: The `MyNatsClient.Rx` NuGet package is no longer needed as a separate package as the `MyNatsClient` NuGet package now takes a dependency on `System.Reactive`.
- **Changed**: `IFilteredObservable<T>` has been dropped as the main package now has access to `System.Reactive` which contains that logic.
- **Changed**: `INatsObservable<T>` has been dropped and `IObservable<T>` is used instead.
- **Changed**: `ObservableOf<T>` is now `SafeObservableOf<T>` and does not call `IObserver<T>.OnError` if a handler causes an exception.
- **Changed**: Custom `DelegatingObserver` has been dropped as the main package now has access to `AnonymousObserver` in `System.Reactive`.
- **Changed**: Constructor for `NatsClient` no longer accepts `Id` as it has no usage.
- **Changed**: All methods in `INatsClient` for subscribing is now called `Sub` or `SubAsync`. Hence, `SubWithObserver(...)`, `SubWithHandler(...)` and `SubWithObservableSubscription(...)` is now `Sub(...)`. And `SubWithObserverAsync(...)`, `SubWithHandlerAsync(...)` and `SubWithObservableSubscriptionAsync(...)` is now `SubAsync(...)`
- **Changed**: Support for statistics defined by `INatsClientStats` has been dropped and is not accessible via `client.Stats` anymore.
- **Changed**: `OnException` hook has been removed.
- **Changed**: `MyNatsClient.Encodings.Json` now uses `DefaultContractResolver` and `DefaultNamingStrategy` instead of `CamelCase`. If you want the latter, this has to be explicitly configured.
- **Changed**: `MyNatsClient.Encodings.Json*  now uses `DateTimeZoneHandling.RoundtripKind` instead of `DateTimeZoneHandling.Utc`. If you want the latter, this has to be explicitly configured.
- **New**: Constructor for `NatsClient` now accepts an optional `ISocketFactory`.
- **New**: `SocketOptions.UseNagleAlgorithm`, defaults to (false).
- **New**: New overloads of `client.Unsub(...)` and `client.UnsubAsync(...)`, which now accepts the `ISubscription`.

## v0.11.0 - 2017-01-26
- **Fixed**: Although it worked as it was, the `RequestAsync` method now uses same patter as other clients, and invokes an automatic `unsub` with `maxMessages=1` so that many responders doesn't make the client get unnecessary replies.

## v0.10.0 - 2017-01-10
- **Added**: New NuGet `MyNatsClient.Encodings.Json` which ads simple `Encode` and `Decode` support for JSON and simplifying extension methods for it.
- **Fixed**: Bug with `PubAsync` variations/overloads that used `IPayload` instead of `byte[]` or `string`.


## v0.9.2 - 2017-01-08
Enabling lost support for .NET4.5+, so now .NET4.5+ is build specifically for .NET (plain old .NET) and .NET Core is build against .NET Standard 1.6

- **Fixed**: Support for .NET4.5 again which was lost in `v0.8.0` where the idea was to support it via .NET Standard.

## v0.9.1 - 2017-01-07
- **Fixed**: `NatsException` does not extend `AggregateException` anymore, as there seem to be an issue with internal stuff in .NET Core trying to reach members that does not exist.
- **Fixed**: `client.RequestAsync(...)` now uses `TaskCompletionSource.SetException` for the time out instead of having the method throw the exception.

## v0.9.0 - 2017-01-06
Focus of this release has been to offer more hooks for exceptions in your handlers.

- **Added**: `client.OpStream.OnException = (op, ex) => {}` as a generic client hook for getting notified of exceptions in handlers for the all OPS stream.
- **Added**: `client.MsgOpStream.OnException = (msg, ex) => {}` as a generic client hook for getting notified of exceptions in handlers for the MsgOP stream.
- **Added**: Any exception caused by an observer will now be logged via a `ILogger.Error` resolved via `LoggerManager.Resolve`, which is a place where you can hook in NLog, Serilog or whatever.

## v0.8.0 - 2017-01-04
Focus has been on stabilizing the experience and we are closing in on getting a stable API. No more planned changes exists.

- **Changed**: `ConnectionInfo.AutoReconnectOnFailure` is now by default `true`, so in case that the internal consumer of a message fails, it will try and auto reconnect.
- **Changed**: A failing observable subscription does no longer remove the subscription. `OnError` for the observer is still called if you have injected that. You can therefore control if it should be terminated or not. This is done, e.g. so that just becase Msg#1 on a certain subject X doesn't prevent Msg#2 against the same subject to be handled. Before, the handler subscription was removed if there was an exception for the first one.
- **Changed**: `NatsOpMediator` no longer extends `IObservable<>` but instead exposes two observable streams via composition. This is probably nothing you have used though.
- **Changed**: `INatsClient` now exposes `INatsConnectionManager` instead of a `ISocketFactory`. You now assign a custom `SocketFactory` to the manager instead of to the client. `client.ConnectionManager.SocketFactory = new MySocketFactory()`
- **Changed**: `INatsClient.State` is **removed**, instead there's a simple `client.IsConnected` to chech instead.
- **Changed**: Some codes in `NatsExceptionCodes` has changed. These are used as value for `NatsException.ExceptionCode`.
- **Changed**: When connecting to a NATS Server, the `CONNECT` (handshake) now sends `pedantic=false` and `protocol:1`
- **Changed**: No more specific compile for .NET4.5 as .NET4.5 is supported via .NET Standard 1.6.
- **Changed**: Made `NatsClient.Stats.OpCount` a `ulong` instead of `long`.
- **Changed**: `ConnectionInfo.SocketOptions.SendTimeOut` now defaults to `10s`. Before it had no timeout.
- **Changed**: Tweaked settings for `ConnectionInfo.SocketOptions.ReceiveTimeoutMs` to be `5s` instead of `10s`.
- **Added**: New setting `ConnectionInfo.SocketOptions.ConnectTimeoutMs` with default of `5s`. So no more long defaults on Windows.
- **Added**: New setting `ConnectionInfo.RequestTimeoutMs` with default of `5s`. That is what will be used if no specific request timeout is passed to `client.RequestAsync`.
- **Added**: Since we now send `protocol:1` in the `CONNECT`, we now get additive information dispatched to the client via whenever a new server is added to the cluster. In the next release that will be used to keep a hot list with the possible servers/hosts for a client to connect to in a cluster. Meaning that you will be able to connect to a seed server.
- **Added**: `NatsServerInfo.Host`, `NatsServerInfo.Port` is now extracted from the info sent from the server.
- **Fixed**: Issue with missed `ConfigureAwait(false)`.
- **Fixed**: Issue with looking at wrong state flag (which now has been removed) while disconnecting.
- **Fixed**: Potential issue with not cleaning some resources when trying to connect to multiple servers where e.g. #1 failed and #2 succeeded.

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
- **Fixed:** `NatsConsumer.Subscribe()` now supports wildcard `"*"` and full wildcard `">"`.

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
