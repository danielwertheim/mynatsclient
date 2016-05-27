using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient.Ops;
using NUnit.Framework;

namespace MyNatsClient.IntegrationTests
{
    public class PubSubTests
    {
        private const int MaxTimeMs = 2000;

        private NatsClient _client1;
        private NatsClient _client2;
        private NatsClient _client3;
        private AutoResetEvent _sync;

        [SetUp]
        public void Init()
        {
            var connectionInfo = new ConnectionInfo(new Host("ubuntu01"))
            {
                AutoRespondToPing = false,
                Verbose = false
            };

            _client1 = new NatsClient("tc1", connectionInfo);
            _client1.Connect();

            _client2 = new NatsClient("tc2", connectionInfo);
            _client2.Connect();

            _client3 = new NatsClient("tc3", connectionInfo);
            _client3.Connect();

            _sync = new AutoResetEvent(false);
        }

        [TearDown]
        public void Clean()
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

            _sync?.Dispose();
            _sync = null;
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
                _sync.Set();
            });
            _client2.Sub(subject, "s1");

            _client3.IncomingOps.OfType<MsgOp>().Subscribe(msg =>
            {
                Interlocked.Increment(ref nr3ReceiveCount);
                _sync.Set();
            });
            await _client3.SubAsync(subject, "s1");

            _client1.Pub(subject, "mess1");
            _sync.WaitOne();
            _sync.WaitOne();

            await _client1.PubAsync(subject, "mess2");
            _sync.WaitOne();
            _sync.WaitOne();

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
                _sync.Set();
            });
            _client1.Sub(subject, "s1");

            _client1.Pub(subject, "mess1");
            _sync.WaitOne();

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
                _sync.Set();
            });
            _client1.Sub(subject, "s1");
            _client1.Sub(subject, "s2");

            _client1.Pub(subject, "mess1");
            _sync.WaitOne();
            _sync.WaitOne();

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
                _sync.Set();
            });
            _client1.IncomingOps.OfType<MsgOp>().Where(m => m.Subject == "Bar").Subscribe(msg =>
            {
                Interlocked.Increment(ref nr1ReceiveCount);
                _sync.Set();
            });
            _client1.Sub("Foo", "s1");
            _client1.Sub("Bar", "s2");

            _client1.Pub("Foo", "mess1");
            _client1.Pub("Bar", "mess2");
            _sync.WaitOne();
            _sync.WaitOne();

            nr1ReceiveCount.Should().Be(2);
        }

        [Test]
        [Timeout(MaxTimeMs)]
        public async Task A_client_Should_be_able_to_unsub_from_a_subject()
        {
            const string subject = "Test";
            var nr1ReceiveCount = 0;
            var nr2ReceiveCount = 0;
            var nr3ReceiveCount = 0;

            _client1.IncomingOps.OfType<MsgOp>().Subscribe(msg =>
            {
                _client1.UnSub("s1");
                Interlocked.Increment(ref nr1ReceiveCount);
                _sync.Set();
            });
            _client1.Sub(subject, "s1");

            _client2.IncomingOps.OfType<MsgOp>().Subscribe(async msg =>
            {
                await _client2.UnSubAsync("s1");
                Interlocked.Increment(ref nr2ReceiveCount);
                _sync.Set();
            });
            _client2.Sub(subject, "s1");

            _client3.IncomingOps.OfType<MsgOp>().Subscribe(msg =>
            {
                Interlocked.Increment(ref nr3ReceiveCount);
                _sync.Set();
            });
            _client3.Sub(subject, "s1");

            _client1.Pub(subject, "mess1");
            _sync.WaitOne();
            _sync.WaitOne();
            _sync.WaitOne();

            _client3.UnSub("s1");
            await DelayAsync();

            _client1.Pub(subject, "mess2");
            await DelayAsync();

            nr1ReceiveCount.Should().Be(1);
            nr2ReceiveCount.Should().Be(1);
            nr3ReceiveCount.Should().Be(1);
        }

        [Test]
        [Timeout(MaxTimeMs)]
        public async Task A_client_Should_be_able_to_auto_unsub_after_n_messages_to_subject()
        {
            const string subject = "Test";
            var nr2ReceiveCount = 0;
            var nr3ReceiveCount = 0;

            _client2.IncomingOps.OfType<MsgOp>().Subscribe(msg =>
            {
                Interlocked.Increment(ref nr2ReceiveCount);
                _sync.Set();
            });
            _client2.Sub(subject, "s1");
            _client2.UnSub("s1", 2);

            _client3.IncomingOps.OfType<MsgOp>().Subscribe(msg =>
            {
                Interlocked.Increment(ref nr3ReceiveCount);
                _sync.Set();
            });
            await _client3.SubAsync(subject, "s1");
            await _client3.UnSubAsync("s1", 2);

            _client1.Pub(subject, "mess1");
            _client1.Pub(subject, "mess2");

            _sync.WaitOne();
            _sync.WaitOne();
            _sync.WaitOne();
            _sync.WaitOne();

            _client1.Pub(subject, "mess3");
            await DelayAsync();

            nr2ReceiveCount.Should().Be(2);
            nr3ReceiveCount.Should().Be(2);
        }

        private async Task DelayAsync() => await Task.Delay(250);
    }
}