using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient.Ops;
using Xunit;

namespace MyNatsClient.IntegrationTests
{
    public class ClientUnSubTests : ClientIntegrationTests
    {
        private NatsClient _client1;
        private NatsClient _client2;
        private NatsClient _client3;

        public ClientUnSubTests()
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

        [Fact]
        public async Task Client_Should_be_able_to_unsub_from_a_subject()
        {
            const string subject = "Test";
            var nr1ReceiveCount = 0;
            var nr2ReceiveCount = 0;
            var nr3ReceiveCount = 0;

            _client1.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                _client1.Unsub("s1");
                Interlocked.Increment(ref nr1ReceiveCount);
                ReleaseOne();
            });
            _client1.SubWithObservableSubscription((SubscriptionInfo) subject, (Func<IFilterableObservable<MsgOp>, IDisposable>) "s1");

            _client2.OpStream.OfType<MsgOp>().Subscribe(async msg =>
            {
                await _client2.UnsubAsync("s1");
                Interlocked.Increment(ref nr2ReceiveCount);
                ReleaseOne();
            });
            _client2.SubWithObservableSubscription((SubscriptionInfo) subject, (Func<IFilterableObservable<MsgOp>, IDisposable>) "s1");

            _client3.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                Interlocked.Increment(ref nr3ReceiveCount);
                ReleaseOne();
            });
            _client3.SubWithObservableSubscription((SubscriptionInfo) subject, (Func<IFilterableObservable<MsgOp>, IDisposable>) "s1");

            _client1.Pub(subject, "mess1");
            WaitOne();
            WaitOne();
            WaitOne();

            _client3.Unsub("s1");
            await DelayAsync();

            _client1.Pub(subject, "mess2");
            await DelayAsync();

            nr1ReceiveCount.Should().Be(1);
            nr2ReceiveCount.Should().Be(1);
            nr3ReceiveCount.Should().Be(1);
        }

        [Fact]
        public async Task Client_Should_be_able_to_auto_unsub_after_n_messages_to_subject()
        {
            const string subject = "Test";
            var nr2ReceiveCount = 0;
            var nr3ReceiveCount = 0;

            _client2.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                Interlocked.Increment(ref nr2ReceiveCount);
                ReleaseOne();
            });
            _client2.SubWithObservableSubscription((SubscriptionInfo) subject, (Func<IFilterableObservable<MsgOp>, IDisposable>) "s1");
            _client2.Unsub("s1", 2);

            _client3.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                Interlocked.Increment(ref nr3ReceiveCount);
                ReleaseOne();
            });
            await _client3.SubWithObservableSubscriptionAsync((SubscriptionInfo) subject, (Func<IFilterableObservable<MsgOp>, IDisposable>) "s1");
            await _client3.UnsubAsync("s1", 2);

            _client1.Pub(subject, "mess1");
            _client1.Pub(subject, "mess2");

            WaitOne();
            WaitOne();
            WaitOne();
            WaitOne();

            _client1.Pub(subject, "mess3");
            await DelayAsync();

            nr2ReceiveCount.Should().Be(2);
            nr3ReceiveCount.Should().Be(2);
        }
    }
}