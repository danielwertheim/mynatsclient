using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient;
using MyNatsClient.Ops;
using MyNatsClient.Rx;
using Xunit;

namespace IntegrationTests
{
    public class PubSubTests : Tests<DefaultContext>, IDisposable
    {
        private NatsClient _client1;
        private NatsClient _client2;
        private NatsClient _client3;
        private Sync _sync;

        public PubSubTests(DefaultContext context)
            : base(context)
        { }

        public void Dispose()
        {
            _sync?.Dispose();
            _sync = null;

            _client1?.Disconnect();
            _client1?.Dispose();
            _client1 = null;

            _client2?.Disconnect();
            _client2?.Dispose();
            _client2 = null;

            _client3?.Disconnect();
            _client3?.Dispose();
            _client3 = null;
        }

        [Fact]
        public async Task Client_Should_be_able_to_publish_When_no_subscribers_exists()
        {
            _client1 = await Context.ConnectClientAsync();

            _client1.Pub("Test", "test message");
            await _client1.PubAsync("Test", "Test message");
        }

        [Fact]
        public async Task Client_Should_be_able_to_publish_and_consume_messages_When_publishing_one_by_one()
        {
            var subject = Context.GenerateSubject();
            var messages = new[]
            {
                "My test string\r\nwith two lines and\ttabs!",
                "Foo bar!",
                "My async test string\r\nwith two lines and\ttabs!",
                "Async Foo bar!"
            };

            _sync = Sync.Max(4);
            _client1 = await Context.ConnectClientAsync();
            _client1.MsgOpStream.Subscribe(msg => _sync.Release(msg));
            _client1.Sub(subject);

            await Context.DelayAsync();

            _client1.Pub(subject, messages[0]);
            _client1.Pub(subject, Encoding.UTF8.GetBytes(messages[1]));
            await _client1.PubAsync(subject, messages[2]);
            await _client1.PubAsync(subject, Encoding.UTF8.GetBytes(messages[3]));

            _sync.WaitForAll();
            _sync.InterceptedCount.Should().Be(messages.Length);
            _sync.Intercepted.Select(m => m.GetPayloadAsString()).ToArray().Should().Contain(messages);
        }

        [Fact]
        public async Task Client_Should_be_able_to_publish_and_consume_messages_When_publishing_batch()
        {
            var subject = Context.GenerateSubject();
            var messages = new[]
            {
                "My test string\r\nwith two lines and\ttabs!",
                "Foo bar!",
                "My async test string\r\nwith two lines and\ttabs!",
                "Async Foo bar!"
            };

            _sync = Sync.Max(4);
            _client1 = await Context.ConnectClientAsync();
            _client1.MsgOpStream.Subscribe(msg => _sync.Release(msg));
            _client1.Sub(subject);

            await Context.DelayAsync();

            _client1.PubMany(async p =>
            {
                p.Pub(subject, messages[0]);
                p.Pub(subject, Encoding.UTF8.GetBytes(messages[1]));
                await p.PubAsync(subject, messages[2]);
                await p.PubAsync(subject, Encoding.UTF8.GetBytes(messages[3]));
            });

            _sync.WaitForAll();
            _sync.InterceptedCount.Should().Be(messages.Length);
            _sync.Intercepted.Select(m => m.GetPayloadAsString()).ToArray().Should().Contain(messages);
        }

        [Fact]
        public async Task Client_Should_dispatch_to_all_subscribed_clients()
        {
            var subject = Context.GenerateSubject();
            var nr1Receive = new ConcurrentQueue<MsgOp>();
            var nr2Receive = new ConcurrentQueue<MsgOp>();
            var nr3Receive = new ConcurrentQueue<MsgOp>();

            _sync = Sync.MaxTwo();
            _client1 = await Context.ConnectClientAsync();
            _client2 = await Context.ConnectClientAsync();
            _client3 = await Context.ConnectClientAsync();

            _client1.OpStream.OfType<MsgOp>().Subscribe(msg => nr1Receive.Enqueue(msg));

            _client2.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                nr2Receive.Enqueue(msg);
                _sync.Release(msg);
            });
            _client2.Sub(subject);

            _client3.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                nr3Receive.Enqueue(msg);
                _sync.Release(msg);
            });
            await _client3.SubAsync(subject);

            await Context.DelayAsync();

            _client1.Pub(subject, "mess1");
            _sync.WaitForAll();

            await _client1.PubAsync(subject, "mess2");
            _sync.WaitForAll();

            _sync.InterceptedCount.Should().Be(4);
            nr1Receive.Count.Should().Be(0);
            nr2Receive.Count.Should().Be(2);
            nr3Receive.Count.Should().Be(2);
        }

        [Fact]
        public async Task Client_Should_be_able_to_publish_to_it_self()
        {
            var subject = Context.GenerateSubject();

            _sync = Sync.MaxOne();
            _client1 = await Context.ConnectClientAsync();
            _client1.OpStream.OfType<MsgOp>().Subscribe(msg => _sync.Release(msg));
            _client1.Sub(subject);

            await Context.DelayAsync();

            _client1.Pub(subject, "mess1");
            _sync.WaitForAll();

            _sync.InterceptedCount.Should().Be(1);
        }

        [Fact]
        public async Task Client_Should_be_able_to_subscribe_to_the_same_subject_twice()
        {
            var subject = Context.GenerateSubject();

            _sync = Sync.MaxTwo();
            _client1 = await Context.ConnectClientAsync();
            _client1.OpStream.OfType<MsgOp>().Subscribe(msg => _sync.Release(msg));
            _client1.Sub(subject);
            _client1.Sub(subject);

            await Context.DelayAsync();

            _client1.Pub(subject, "mess1");
            _sync.WaitForAll();

            _sync.InterceptedCount.Should().Be(2);
        }

        [Fact]
        public async Task Client_Should_be_able_to_subscribe_to_many_subjects()
        {
            var subject1 = Context.GenerateSubject();
            var subject2 = Context.GenerateSubject();

            _sync = Sync.MaxTwo();
            _client1 = await Context.ConnectClientAsync();
            _client1.OpStream.OfType<MsgOp>().Where(m => m.Subject == subject1).Subscribe(msg => _sync.Release(msg));
            _client1.OpStream.OfType<MsgOp>().Where(m => m.Subject == subject2).Subscribe(msg => _sync.Release(msg));
            _client1.Sub(subject1);
            _client1.Sub(subject2);

            await Context.DelayAsync();

            _client1.Pub(subject1, "mess1");
            _client1.Pub(subject2, "mess2");
            _sync.WaitForAll();

            _sync.InterceptedCount.Should().Be(2);
        }
        
        [Fact]
        public async Task Client_Should_throw_if_pub_when_never_connected()
        {
            var subject = Context.GenerateSubject();
            var body = new byte[0];

            _client1 = Context.CreateClient();

            Should.ThrowNatsException(() => _client1.Pub(subject, "body"));
            Should.ThrowNatsException(() => _client1.Pub(subject, "body", "reply.to.subject"));
            Should.ThrowNatsException(() => _client1.Pub(subject, body.AsMemory()));
            Should.ThrowNatsException(() => _client1.Pub(subject, body.AsMemory(), "reply.to.subject"));

            await Should.ThrowNatsExceptionAsync(() => _client1.PubAsync(subject, "body"));
            await Should.ThrowNatsExceptionAsync(() => _client1.PubAsync(subject, "body", "reply.to.subject"));
            await Should.ThrowNatsExceptionAsync(() => _client1.PubAsync(subject, body.AsMemory()));
            await Should.ThrowNatsExceptionAsync(() => _client1.PubAsync(subject, body.AsMemory(), "reply.to.subject"));
        }

        [Fact]
        public async Task Client_Should_throw_if_pub_after_disconnected()
        {
            var subject = Context.GenerateSubject();
            var body = new byte[0];

            _client1 = await Context.ConnectClientAsync();

            // Succeeds
            _client1.Pub(subject, "body");
            _client1.Pub(subject, "body", "repy.to.subject");
            _client1.Pub(subject, body.AsMemory());
            _client1.Pub(subject, body.AsMemory(), "repy.to.subject");

            // Disconnect from NATS per user request
            _client1.Disconnect();
            Assert.False(_client1.IsConnected);

            // Fails after being disconnected
            Should.ThrowNatsException(() => _client1.Pub(subject, "body"));
            Should.ThrowNatsException(() => _client1.Pub(subject, "body", "reply.to.subject"));
            Should.ThrowNatsException(() => _client1.Pub(subject, body.AsMemory()));
            Should.ThrowNatsException(() => _client1.Pub(subject, body.AsMemory(), "reply.to.subject"));

            await Should.ThrowNatsExceptionAsync(() => _client1.PubAsync(subject, "body"));
            await Should.ThrowNatsExceptionAsync(() => _client1.PubAsync(subject, "body", "reply.to.subject"));
            await Should.ThrowNatsExceptionAsync(() => _client1.PubAsync(subject, body.AsMemory()));
            await Should.ThrowNatsExceptionAsync(() => _client1.PubAsync(subject, body.AsMemory(), "reply.to.subject"));
        }
    }
}
