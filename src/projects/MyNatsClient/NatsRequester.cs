using System;
using System.Threading.Tasks;
using EnsureThat;
using MyNatsClient.Internals;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public class NatsRequester : IDisposable
    {
        private readonly string _clientInbox;
        private readonly INatsClient _client;
        private readonly IClientSubscription _inboxSubscription;
        private ObservableOf<MsgOp> _responses;

        public NatsRequester(INatsClient client)
        {
            EnsureArg.IsNotNull(client, nameof(client));

            _client = client;
            _clientInbox = Guid.NewGuid().ToString("N");
            _responses = new ObservableOf<MsgOp>();
            _inboxSubscription = _client.SubWithHandler($"{_clientInbox}.>", msg => _responses.Dispatch(msg));
        }

        public async Task<MsgOp> RequestAsync(string subject, string body)
        {
            var requestId = Guid.NewGuid().ToString("N");
            var taskComp = new TaskCompletionSource<MsgOp>();
            var requestSubscription = _responses.Subscribe(
                new DelegatingObserver<MsgOp>(msg =>
                {
                    Console.WriteLine(1);
                    taskComp.SetResult(msg);
                }),
                msg => msg.Subject == $"{_clientInbox}.{requestId}");

            await _client.PubAsync(subject, body, $"{_clientInbox}.{requestId}").ConfigureAwait(false);

            return await taskComp.Task.ContinueWith(t =>
            {
                requestSubscription?.Dispose();
                return t.Result;
            }).ConfigureAwait(false);
        }

        public void Dispose()
        {
            Try.All(
                () =>
                {
                    _inboxSubscription?.Dispose();
                },
                () =>
                {
                    _responses?.Dispose();
                    _responses = null;
                });
        }
    }
}