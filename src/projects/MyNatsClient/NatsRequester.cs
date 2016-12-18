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
        private ObservableOf<MsgOp> _responses;

        public NatsRequester(INatsClient client)
        {
            EnsureArg.IsNotNull(client, nameof(client));

            _client = client;
            _clientInbox = Guid.NewGuid().ToString("N");
            _responses = new ObservableOf<MsgOp>();
            _client.Sub($"{_clientInbox}.>", msg => _responses.Dispatch(msg));
        }

        public async Task<MsgOp> RequestAsync(string subject, string body)
        {
            var taskComp = new TaskCompletionSource<MsgOp>();
            var requestId = Guid.NewGuid().ToString("N");
            var subscription = _responses.Subscribe(
                new DelegatingObserver<MsgOp>(msg =>
                {
                    Console.WriteLine(2);
                    taskComp.SetResult(msg);
                }),
                msg => msg.Subject == $"{_clientInbox}.{requestId}");

            await _client.PubAsync(subject, body, $"{_clientInbox}.{requestId}").ConfigureAwait(false);

            return await taskComp.Task.ContinueWith(t =>
            {
                subscription?.Dispose();
                return t.Result;
            });
        }

        public void Dispose()
        {
            Try.All(
                () =>
                {
                    _client.Unsub(_clientInbox);
                },
                () =>
                {
                    _responses?.Dispose();
                    _responses = null;
                });
        }
    }
}