using System;
using System.Threading.Tasks;
using EnsureThat;
using MyNatsClient.Internals;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public class NatsRequester : IDisposable
    {
        private readonly INatsClient _client;
        private ClientInbox _inbox;

        public NatsRequester(INatsClient client)
        {
            EnsureArg.IsNotNull(client, nameof(client));

            _client = client;
            _inbox = new ClientInbox(client);
        }

        public async Task<MsgOp> RequestAsync(string subject, string body)
        {
            var requestId = Guid.NewGuid().ToString("N");
            var taskComp = new TaskCompletionSource<MsgOp>();
            var requestSubscription = _inbox.Responses.Subscribe(
                new DelegatingObserver<MsgOp>(msg =>
                {
                    Console.WriteLine(1);
                    taskComp.SetResult(msg);
                }),
                msg => msg.Subject == $"{_inbox.Address}.{requestId}");

            await _client.PubAsync(subject, body, $"{_inbox.Address}.{requestId}").ConfigureAwait(false);

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
                    _inbox?.Dispose();
                    _inbox = null;
                });
        }
    }
}