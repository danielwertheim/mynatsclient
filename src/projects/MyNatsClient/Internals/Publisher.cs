using System;
using System.Threading.Tasks;
using MyNatsClient.Internals.Commands;

namespace MyNatsClient.Internals
{
    internal class Publisher : IPublisher
    {
        private readonly Action<byte[]> _pubBytesSync;
        private readonly Func<byte[], Task> _pubBytesAsync;
        private readonly Action<IPayload> _pubPayloadSync;
        private readonly Func<IPayload, Task> _pubPayloadAsync;

        internal Publisher(
            Action<byte[]> pubBytesSync,
            Func<byte[], Task> pubBytesAsync,
            Action<IPayload> pubPayloadSync,
            Func<IPayload, Task> pubPayloadAsync)
        {
            _pubBytesSync = pubBytesSync;
            _pubBytesAsync = pubBytesAsync;
            _pubPayloadSync = pubPayloadSync;
            _pubPayloadAsync = pubPayloadAsync;
        }

        public void Pub(string subject, string body, string replyTo = null)
            => _pubBytesSync(PubCmd.Generate(subject, body, replyTo));

        public void Pub(string subject, byte[] body, string replyTo = null)
            => _pubBytesSync(PubCmd.Generate(subject, body, replyTo));

        public void Pub(string subject, IPayload body, string replyTo = null)
            => _pubPayloadSync(PubCmd.Generate(subject, body, replyTo));

        public async Task PubAsync(string subject, string body, string replyTo = null)
            => await _pubBytesAsync(PubCmd.Generate(subject, body, replyTo)).ConfigureAwait(false);

        public async Task PubAsync(string subject, byte[] body, string replyTo = null)
            => await _pubBytesAsync(PubCmd.Generate(subject, body, replyTo)).ConfigureAwait(false);

        public async Task PubAsync(string subject, IPayload body, string replyTo = null)
            => await _pubPayloadAsync(PubCmd.Generate(subject, body, replyTo)).ConfigureAwait(false);
    }
}