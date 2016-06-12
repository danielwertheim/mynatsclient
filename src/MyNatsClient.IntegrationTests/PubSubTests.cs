using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient.Ops;
using NUnit.Framework;

namespace MyNatsClient.IntegrationTests
{
    public class PubSubTests : ClientIntegrationTests
    {
        private NatsClient _client1;
        private NatsClient _client2;
        private NatsClient _client3;

        protected override void OnBeforeEachTest()
        {
            _client1 = new NatsClient("tc1", ConnectionInfo);
            _client1.Connect();

            _client2 = new NatsClient("tc2", ConnectionInfo);
            _client2.Connect();

            _client3 = new NatsClient("tc3", ConnectionInfo);
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

        [Test]
        [Timeout(MaxTimeMs)]
        public async Task A_client_publishing_Should_succeed_When_no_subscribers_exists()
        {
            _client1.Pub("Test", "test message");
            await _client1.PubAsync("Test", "Test message");
        }

        [Test]
        [Timeout(MaxTimeMs)]
        public async Task A_published_message_Should_be_published_and_consumed_in_full()
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
                    Sync.Set();
            });
            _client1.Sub("Test", "s1");

            _client1.Pub("Test", messages[0]);
            _client1.Pub("Test", Encoding.UTF8.GetBytes(messages[1]));
            await _client1.PubAsync("Test", messages[2]);
            await _client1.PubAsync("Test", Encoding.UTF8.GetBytes(messages[3]));

            Sync.WaitOne();
            intercepted.Should().HaveCount(messages.Length);
            intercepted.Select(m => m.GetPayloadAsString()).ToArray().Should().Contain(messages);
        }

        [Test]
        [Timeout(MaxTimeMs)]
        public void A_published_message_using_PubMany_Should_be_published_and_consumed_in_full()
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
                    Sync.Set();
            });
            _client1.Sub("Test", "s1");

            _client1.PubMany(async p =>
            {
                p.Pub("Test", messages[0]);
                p.Pub("Test", Encoding.UTF8.GetBytes(messages[1]));
                await p.PubAsync("Test", messages[2]);
                await p.PubAsync("Test", Encoding.UTF8.GetBytes(messages[3]));
            });

            Sync.WaitOne();
            intercepted.Should().HaveCount(messages.Length);
            intercepted.Select(m => m.GetPayloadAsString()).ToArray().Should().Contain(messages);
        }

        [Test]
        [Timeout(MaxTimeMs)]
        public async Task A_client_publishing_Should_dispatch_to_all_subscribed_clients()
        {
            const string subject = "Test";
            var nr2ReceiveCount = 0;
            var nr3ReceiveCount = 0;

            _client2.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                Interlocked.Increment(ref nr2ReceiveCount);
                Sync.Set();
            });
            _client2.Sub(subject, "s1");

            _client3.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                Interlocked.Increment(ref nr3ReceiveCount);
                Sync.Set();
            });
            await _client3.SubAsync(subject, "s1");

            _client1.Pub(subject, "mess1");
            Sync.WaitOne();
            Sync.WaitOne();

            await _client1.PubAsync(subject, "mess2");
            Sync.WaitOne();
            Sync.WaitOne();

            nr2ReceiveCount.Should().Be(2);
            nr3ReceiveCount.Should().Be(2);
        }

        [Test]
        [Timeout(MaxTimeMs)]
        public void A_client_publishing_Should_be_able_to_publish_to_it_self()
        {
            const string subject = "Test";
            var nr1ReceiveCount = 0;

            _client1.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                Interlocked.Increment(ref nr1ReceiveCount);
                Sync.Set();
            });
            _client1.Sub(subject, "s1");

            _client1.Pub(subject, "mess1");
            Sync.WaitOne();

            nr1ReceiveCount.Should().Be(1);
        }

        [Test]
        [Timeout(MaxTimeMs)]
        public void A_client_Should_be_able_to_subscribe_to_the_same_subject_twice()
        {
            const string subject = "Test";
            var nr1ReceiveCount = 0;

            _client1.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                Interlocked.Increment(ref nr1ReceiveCount);
                Sync.Set();
            });
            _client1.Sub(subject, "s1");
            _client1.Sub(subject, "s2");

            _client1.Pub(subject, "mess1");
            Sync.WaitOne();
            Sync.WaitOne();

            nr1ReceiveCount.Should().Be(2);
        }

        [Test]
        [Timeout(MaxTimeMs)]
        public void A_client_Should_be_able_to_subscribe_to_many_subjects()
        {
            var nr1ReceiveCount = 0;

            _client1.OpStream.OfType<MsgOp>().Where(m => m.Subject == "Foo").Subscribe(msg =>
            {
                Interlocked.Increment(ref nr1ReceiveCount);
                Sync.Set();
            });
            _client1.OpStream.OfType<MsgOp>().Where(m => m.Subject == "Bar").Subscribe(msg =>
            {
                Interlocked.Increment(ref nr1ReceiveCount);
                Sync.Set();
            });
            _client1.Sub("Foo", "s1");
            _client1.Sub("Bar", "s2");

            _client1.Pub("Foo", "mess1");
            _client1.Pub("Bar", "mess2");
            Sync.WaitOne();
            Sync.WaitOne();

            nr1ReceiveCount.Should().Be(2);
        }
    }
}