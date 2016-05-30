using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient.Ops;
using NUnit.Framework;

namespace MyNatsClient.IntegrationTests
{
    public class UnSubTests : ClientIntegrationTests
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
                Sync.Set();
            });
            _client1.Sub(subject, "s1");

            _client2.IncomingOps.OfType<MsgOp>().Subscribe(async msg =>
            {
                await _client2.UnSubAsync("s1");
                Interlocked.Increment(ref nr2ReceiveCount);
                Sync.Set();
            });
            _client2.Sub(subject, "s1");

            _client3.IncomingOps.OfType<MsgOp>().Subscribe(msg =>
            {
                Interlocked.Increment(ref nr3ReceiveCount);
                Sync.Set();
            });
            _client3.Sub(subject, "s1");

            _client1.Pub(subject, "mess1");
            Sync.WaitOne();
            Sync.WaitOne();
            Sync.WaitOne();

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
                Sync.Set();
            });
            _client2.Sub(subject, "s1");
            _client2.UnSub("s1", 2);

            _client3.IncomingOps.OfType<MsgOp>().Subscribe(msg =>
            {
                Interlocked.Increment(ref nr3ReceiveCount);
                Sync.Set();
            });
            await _client3.SubAsync(subject, "s1");
            await _client3.UnSubAsync("s1", 2);

            _client1.Pub(subject, "mess1");
            _client1.Pub(subject, "mess2");

            Sync.WaitOne();
            Sync.WaitOne();
            Sync.WaitOne();
            Sync.WaitOne();

            _client1.Pub(subject, "mess3");
            await DelayAsync();

            nr2ReceiveCount.Should().Be(2);
            nr3ReceiveCount.Should().Be(2);
        }
    }
}