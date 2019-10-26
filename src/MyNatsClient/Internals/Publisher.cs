using System;
using System.Threading.Tasks;
using MyNatsClient.Internals.Commands;

namespace MyNatsClient.Internals
{
    internal class Publisher : IPublisher
    {
        private readonly INatsStreamWriter _writer;
        private readonly int _maxPayload;

        internal Publisher(INatsStreamWriter writer, int maxPayload)
        {
            _writer = writer;
            _maxPayload = maxPayload;
        }

        public void Pub(string subject, string body, string replyTo = null)
        {
            var payload = NatsEncoder.GetBytes(body);
            if (payload.Length > _maxPayload)
                throw NatsException.ExceededMaxPayload(_maxPayload, payload.Length);

            PubCmd.Write(_writer, subject, replyTo, payload);
        }

        public void Pub(string subject, ReadOnlyMemory<byte> body, string replyTo = null)
        {
            if (body.Length > _maxPayload)
                throw NatsException.ExceededMaxPayload(_maxPayload, body.Length);

            PubCmd.Write(_writer, subject, replyTo, body);
        }

        public Task PubAsync(string subject, string body, string replyTo = null)
        {
            var payload = NatsEncoder.GetBytes(body);
            if (payload.Length > _maxPayload)
                throw NatsException.ExceededMaxPayload(_maxPayload, payload.Length);

            return PubCmd.WriteAsync(_writer, subject.AsMemory(), replyTo.AsMemory(), payload);
        }

        public Task PubAsync(string subject, ReadOnlyMemory<byte> body, string replyTo = null)
        {
            if (body.Length > _maxPayload)
                throw NatsException.ExceededMaxPayload(_maxPayload, body.Length);

            return PubCmd.WriteAsync(_writer, subject.AsMemory(), replyTo.AsMemory(), body);
        }
    }
}