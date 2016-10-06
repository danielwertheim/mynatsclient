# Release notes

## v0.3.0 - 2016-10-xx
- New: Support for DotNet Core.
- New: You can now have credentials on a specific host instead of common credentials. Specific overrides common.
- Fixed: `client.Pub` methods had `replyTo` as mandatory argument but it is optional.

## v0.2.1 - 2016-06-14
- Closed: [#7](https://github.com/danielwertheim/mynatsclient/issues/9), remove dependency on JSON.Net
- Closed: [#10](https://github.com/danielwertheim/mynatsclient/issues/9), provide MyNatsClient.Rx as a convenience NuGet for bringing in MyNatsClient and Rx.

## v0.1.2 - 2016-06-12
- Fixed: [#9](https://github.com/danielwertheim/mynatsclient/issues/9), with mix up of QueueGroup and ReplyTo

## v0.1.1 - 2016-06-12
- Fixed: Wrong dependencies in NuSpec

## v0.1.0 - 2016-06-12
- First release.