using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient;
using MyNatsClient.Ops;
using MyNatsClient.Rx;
using Xunit;

namespace IntegrationTests
{
    public class RequestTests : Tests<DefaultContext>, IDisposable
    {
        private NatsClient _requester;
        private NatsClient _responder;

        public RequestTests(DefaultContext context)
            : base(context)
        {
        }
        
        public void Dispose()
        {
            _requester?.Disconnect();
            _requester?.Dispose();
            _requester = null;

            _responder?.Disconnect();
            _responder?.Dispose();
            _responder = null;
        }

        [Fact]
        public async Task Given_responder_exists_When_requesting_using_string_It_should_get_response()
        {
            var value = Guid.NewGuid().ToString("N");

            _responder = await Context.ConnectClientAsync();
            _requester = await Context.ConnectClientAsync();

            _responder.Sub("getValue", stream => stream.Subscribe(msg => _responder.Pub(msg.ReplyTo, msg.GetPayloadAsString())));

            var response = await _requester.RequestAsync("getValue", value);

            response.GetPayloadAsString().Should().Be(value);
        }

        [Fact]
        public async Task Given_responder_exists_When_requesting_using_bytes_It_should_get_response()
        {
            var value = Guid.NewGuid().ToString("N");

            _responder = await Context.ConnectClientAsync();
            _requester = await Context.ConnectClientAsync();

            _responder.Sub("getValue", stream => stream.Subscribe(msg => _responder.Pub(msg.ReplyTo, msg.GetPayloadAsString())));

            var response = await _requester.RequestAsync("getValue", Encoding.UTF8.GetBytes(value));

            response.GetPayloadAsString().Should().Be(value);
        }

        [Fact]
        public async Task Given_multiple_responders_exists_and_inbox_requests_are_used_When_requesting_It_should_call_requester_twice_but_dispatch_one_response()
        {
            var cnInfoResponder = Context.GetConnectionInfo();
            var cnInfoRequester = cnInfoResponder.Clone();
            cnInfoRequester.UseInboxRequests = true;

            var value = Guid.NewGuid().ToString("N");
            var responderReceived = new ConcurrentQueue<MsgOp>();
            var requesterReceived = new ConcurrentQueue<MsgOp>();
            var responsesReceived = new ConcurrentQueue<MsgOp>();

            _responder = await Context.ConnectClientAsync(cnInfoResponder);
            _requester = await Context.ConnectClientAsync(cnInfoRequester);

            _requester.MsgOpStream.Subscribe(msgOp => requesterReceived.Enqueue(msgOp));

            _responder.Sub("getValue", stream => stream.Subscribe(msg =>
            {
                responderReceived.Enqueue(msg);
                _responder.Pub(msg.ReplyTo, msg.GetPayloadAsString());
            }));

            using (var responder2 = new NatsClient(cnInfoResponder))
            {
                await responder2.ConnectAsync();
                responder2.Sub("getValue", stream => stream.Subscribe(msg =>
                {
                    responderReceived.Enqueue(msg);
                    responder2.Pub(msg.ReplyTo, msg.GetPayloadAsString());
                }));

                var response = await _requester.RequestAsync("getValue", value);
                responsesReceived.Enqueue(response);
            }

            responsesReceived.Should().HaveCount(1);
            requesterReceived.Should().HaveCount(2);
            responderReceived.Should().HaveCount(2);
            responsesReceived.First().GetPayloadAsString().Should().Be(value);
        }

        [Fact]
        public async Task Given_multiple_responders_exists_and_non_inbox_requests_are_used_When_requesting_It_should_call_requester_once_and_dispatch_one_response()
        {
            var cnInfoResponder = Context.GetConnectionInfo();
            var cnInfoRequester = cnInfoResponder.Clone();
            cnInfoRequester.UseInboxRequests = false;

            var value = Guid.NewGuid().ToString("N");
            var responderReceived = new ConcurrentQueue<MsgOp>();
            var requesterReceived = new ConcurrentQueue<MsgOp>();
            var responsesReceived = new ConcurrentQueue<MsgOp>();

            _responder = await Context.ConnectClientAsync(cnInfoResponder);
            _requester = await Context.ConnectClientAsync(cnInfoRequester);

            _requester.MsgOpStream.Subscribe(msgOp => requesterReceived.Enqueue(msgOp));

            _responder.Sub("getValue", stream => stream.Subscribe(msg =>
            {
                responderReceived.Enqueue(msg);
                _responder.Pub(msg.ReplyTo, msg.GetPayloadAsString());
            }));

            using (var responder2 = new NatsClient(cnInfoResponder))
            {
                await responder2.ConnectAsync();
                responder2.Sub("getValue", stream => stream.Subscribe(msg =>
                {
                    responderReceived.Enqueue(msg);
                    responder2.Pub(msg.ReplyTo, msg.GetPayloadAsString());
                }));

                var response = await _requester.RequestAsync("getValue", value);
                responsesReceived.Enqueue(response);
            }

            responsesReceived.Should().HaveCount(1);
            requesterReceived.Should().HaveCount(1);
            responderReceived.Should().HaveCount(2);
            responsesReceived.First().GetPayloadAsString().Should().Be(value);
        }

        [Fact]
        public async Task Given_no_responder_exists_When_requesting_It_should_get_cancelled()
        {
            _requester = await Context.ConnectClientAsync();

            Func<Task> a = async () => await _requester.RequestAsync("getValue", "foo value");

            a.Should().Throw<TaskCanceledException>();
        }

        [Fact]
        public async Task Given_no_responder_exists_When_requesting_with_explicit_time_out_It_should_get_cancelled()
        {
            _requester = await Context.ConnectClientAsync();

            Func<Task> a = async () =>
            {
                var cts = new CancellationTokenSource(100);
                await _requester.RequestAsync("getValue", "foo value", cts.Token);
            };

            a.Should().Throw<TaskCanceledException>();
        }
    }
}