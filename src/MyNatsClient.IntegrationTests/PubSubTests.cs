using System;
using System.Reactive.Linq;
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
        public async Task A_client_publishing_Should_dispatch_to_all_subscribed_clients()
        {
            const string subject = "Test";
            var nr2ReceiveCount = 0;
            var nr3ReceiveCount = 0;

            _client2.IncomingOps.OfType<MsgOp>().Subscribe(msg =>
            {
                Interlocked.Increment(ref nr2ReceiveCount);
                Sync.Set();
            });
            _client2.Sub(subject, "s1");

            _client3.IncomingOps.OfType<MsgOp>().Subscribe(msg =>
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

            _client1.IncomingOps.OfType<MsgOp>().Subscribe(msg =>
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

            _client1.IncomingOps.OfType<MsgOp>().Subscribe(msg =>
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

            _client1.IncomingOps.OfType<MsgOp>().Where(m => m.Subject == "Foo").Subscribe(msg =>
            {
                Interlocked.Increment(ref nr1ReceiveCount);
                Sync.Set();
            });
            _client1.IncomingOps.OfType<MsgOp>().Where(m => m.Subject == "Bar").Subscribe(msg =>
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