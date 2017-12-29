using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient;
using MyNatsClient.Ops;
using Xunit;

namespace IntegrationTests
{
    public class ClientPubSubTests : ClientIntegrationTests
    {
        private NatsClient _client1;
        private NatsClient _client2;
        private NatsClient _client3;

        public ClientPubSubTests()
        {
            _client1 = new NatsClient(ConnectionInfo);
            _client1.Connect();

            _client2 = new NatsClient(ConnectionInfo);
            _client2.Connect();

            _client3 = new NatsClient(ConnectionInfo);
            _client3.Connect();
        }

        protected override void OnAfterEachTest()
        {
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
            _client1.Pub("Test", "test message");
            await _client1.PubAsync("Test", "Test message");
        }

        [Fact]
        public async Task Client_Should_be_able_to_publish_and_consume_messages_When_publishing_one_by_one()
        {
            var interceptCount = 0;
            var intercepted = new List<MsgOp>();
            var messages = new[]
            {
                "My test string\r\nwith two lines and\ttabs!",
                "Foo bar!",
                "My async test string\r\nwith two lines and\ttabs!",
                "Async Foo bar!"
            };
            _client1.MsgOpStream.Subscribe(msg =>
            {
                intercepted.Add(msg);
                var x = Interlocked.Increment(ref interceptCount);
                if (x == messages.Length)
                    ReleaseOne();
            });
            _client1.Sub("Test");

            _client1.Pub("Test", messages[0]);
            _client1.Pub("Test", Encoding.UTF8.GetBytes(messages[1]));
            await _client1.PubAsync("Test", messages[2]);
            await _client1.PubAsync("Test", Encoding.UTF8.GetBytes(messages[3]));

            WaitOne();
            intercepted.Should().HaveCount(messages.Length);
            intercepted.Select(m => m.GetPayloadAsString()).ToArray().Should().Contain(messages);
        }

        [Fact]
        public void Client_Should_be_able_to_publish_and_consume_messages_When_publishing_batch()
        {
            var interceptCount = 0;
            var intercepted = new List<MsgOp>();
            var messages = new[]
            {
                "My test string\r\nwith two lines and\ttabs!",
                "Foo bar!",
                "My async test string\r\nwith two lines and\ttabs!",
                "Async Foo bar!"
            };
            _client1.MsgOpStream.Subscribe(msg =>
            {
                intercepted.Add(msg);
                var x = Interlocked.Increment(ref interceptCount);
                if (x == messages.Length)
                    ReleaseOne();
            });
            _client1.Sub("Test");

            _client1.PubMany(async p =>
            {
                p.Pub("Test", messages[0]);
                p.Pub("Test", Encoding.UTF8.GetBytes(messages[1]));
                await p.PubAsync("Test", messages[2]);
                await p.PubAsync("Test", Encoding.UTF8.GetBytes(messages[3]));
            });

            WaitOne();
            intercepted.Should().HaveCount(messages.Length);
            intercepted.Select(m => m.GetPayloadAsString()).ToArray().Should().Contain(messages);
        }

        [Fact]
        public async Task Client_Should_dispatch_to_all_subscribed_clients()
        {
            const string subject = "Test";
            var nr2ReceiveCount = 0;
            var nr3ReceiveCount = 0;

            _client2.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                Interlocked.Increment(ref nr2ReceiveCount);
                ReleaseOne();
            });
            _client2.Sub(subject);

            _client3.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                Interlocked.Increment(ref nr3ReceiveCount);
                ReleaseOne();
            });
            await _client3.SubAsync(subject);

            _client1.Pub(subject, "mess1");
            WaitOne();
            WaitOne();

            await _client1.PubAsync(subject, "mess2");
            WaitOne();
            WaitOne();

            nr2ReceiveCount.Should().Be(2);
            nr3ReceiveCount.Should().Be(2);
        }

        [Fact]
        public void Client_Should_be_able_to_publish_to_it_self()
        {
            const string subject = "Test";
            var nr1ReceiveCount = 0;

            _client1.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                Interlocked.Increment(ref nr1ReceiveCount);
                ReleaseOne();
            });
            _client1.Sub(subject);

            _client1.Pub(subject, "mess1");
            WaitOne();

            nr1ReceiveCount.Should().Be(1);
        }

        [Fact]
        public void Client_Should_be_able_to_subscribe_to_the_same_subject_twice()
        {
            const string subject = "Test";
            var nr1ReceiveCount = 0;

            _client1.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                Interlocked.Increment(ref nr1ReceiveCount);
                ReleaseOne();
            });
            _client1.Sub(subject);
            _client1.Sub(subject);

            _client1.Pub(subject, "mess1");
            WaitOne();
            WaitOne();

            nr1ReceiveCount.Should().Be(2);
        }

        [Fact]
        public void Client_Should_be_able_to_subscribe_to_many_subjects()
        {
            var nr1ReceiveCount = 0;

            _client1.OpStream.OfType<MsgOp>().Where(m => m.Subject == "Foo").Subscribe(msg =>
            {
                Interlocked.Increment(ref nr1ReceiveCount);
                ReleaseOne();
            });
            _client1.OpStream.OfType<MsgOp>().Where(m => m.Subject == "Bar").Subscribe(msg =>
            {
                Interlocked.Increment(ref nr1ReceiveCount);
                ReleaseOne();
            });
            _client1.Sub("Foo");
            _client1.Sub("Bar");

            _client1.Pub("Foo", "mess1");
            _client1.Pub("Bar", "mess2");
            WaitOne();
            WaitOne();

            nr1ReceiveCount.Should().Be(2);
        }
    }
}