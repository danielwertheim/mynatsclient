using System;
using System.Threading.Tasks;
using EnsureThat;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public class NatsRequester : IDisposable
    {
        private readonly string _clientInbox;
        private readonly INatsClient _client;

        public NatsRequester(INatsClient client)
        {
            EnsureArg.IsNotNull(client, nameof(client));

            _client = client;
            _clientInbox = Guid.NewGuid().ToString("N");
            _client.Sub($"{_clientInbox}.>", _clientInbox);
        }

        public async Task<MsgOp> RequestAsync(string subject, string body)
        {
            var taskComp = new TaskCompletionSource<MsgOp>();
            var requestId = Guid.NewGuid().ToString("N");
            var subscription = _client.MsgOpStream.Subscribe(
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

        public void Dispose() => _client.Unsub(_clientInbox);
    }
}