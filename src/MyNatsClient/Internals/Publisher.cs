using System;
using System.Threading.Tasks;
using MyNatsClient.Internals.Commands;
using MyNatsClient.Internals.Extensions;

namespace MyNatsClient.Internals
{
    internal class Publisher : IPublisher
    {
        private readonly Action<byte[]> _ps;
        private readonly Func<byte[], Task> _psa;

        internal Publisher(
            Action<byte[]> ps,
            Func<byte[], Task> psa)
        {
            _ps = ps;
            _psa = psa;
        }

        public void Pub(string subject, string body, string replyTo = null)
            => _ps(PubCmd.Generate(subject, body, replyTo));

        public void Pub(string subject, byte[] body, string replyTo = null)
            => _ps(PubCmd.Generate(subject, body, replyTo));

        public async Task PubAsync(string subject, string body, string replyTo = null)
            => await _psa(PubCmd.Generate(subject, body, replyTo)).ForAwait();

        public async Task PubAsync(string subject, byte[] body, string replyTo = null)
            => await _psa(PubCmd.Generate(subject, body, replyTo)).ForAwait();
    }
}