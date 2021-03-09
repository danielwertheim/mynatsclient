using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient;
using MyNatsClient.Ops;
using MyNatsClient.Rx;
using Xunit;

namespace IntegrationTests
{
    public class UnSubTests : Tests<DefaultContext>, IDisposable
    {
        private NatsClient _client1;
        private NatsClient _client2;
        private NatsClient _client3;
        private Sync _sync;

        public UnSubTests(DefaultContext context)
            : base(context)
        {
        }

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

        private async Task ConnectAllClients()
        {
            _client1 = await Context.ConnectClientAsync();
            _client2 = await Context.ConnectClientAsync();
            _client3 = await Context.ConnectClientAsync();
        }

        [Fact]
        public async Task Client_Should_be_able_to_unsub_from_a_subject()
        {
            var subject = Context.GenerateSubject();
            var nr1Received = new ConcurrentQueue<MsgOp>();
            var nr2Received = new ConcurrentQueue<MsgOp>();
            var nr3Received = new ConcurrentQueue<MsgOp>();
            var subInfo1 = new SubscriptionInfo(subject);
            var subInfo2 = new SubscriptionInfo(subject);
            var subInfo3 = new SubscriptionInfo(subject);

            _sync = Sync.MaxThree();
            await ConnectAllClients();

            _client1.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                _client1.Unsub(subInfo1);
                nr1Received.Enqueue(msg);
                _sync.Release(msg);
            });
            _client1.Sub(subInfo1);

            _client2.OpStream.OfType<MsgOp>().Subscribe(async msg =>
            {
                await _client2.UnsubAsync(subInfo2);
                nr2Received.Enqueue(msg);
                _sync.Release(msg);
            });
            _client2.Sub(subInfo2);

            _client3.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                nr3Received.Enqueue(msg);
                _sync.Release(msg);
            });
            _client3.Sub(subInfo3);

            await Context.DelayAsync();

            _client1.Pub(subject, "mess1");
            _sync.WaitForAll();

            _client3.Unsub(subInfo3);
            await Context.DelayAsync();

            _client1.Pub(subject, "mess2");
            await Context.DelayAsync();

            _sync.InterceptedCount.Should().Be(3);
            nr1Received.Should().HaveCount(1);
            nr2Received.Should().HaveCount(1);
            nr3Received.Should().HaveCount(1);
        }

        [Fact]
        public async Task Client_Should_be_able_to_unsub_from_a_subject_by_passing_a_subscription_object()
        {
            var subject = Context.GenerateSubject();
            var nr1Received = new ConcurrentQueue<MsgOp>();
            var nr2Received = new ConcurrentQueue<MsgOp>();
            var nr3Received = new ConcurrentQueue<MsgOp>();
            var subInfo1 = new SubscriptionInfo(subject);
            var subInfo2 = new SubscriptionInfo(subject);
            var subInfo3 = new SubscriptionInfo(subject);

            _sync = Sync.MaxThree();
            await ConnectAllClients();

            _client1.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                nr1Received.Enqueue(msg);
                _sync.Release(msg);
            });
            var sub1 = _client1.Sub(subInfo1);

            _client2.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                nr2Received.Enqueue(msg);
                _sync.Release(msg);
            });
            var sub2 = _client2.Sub(subInfo2);

            _client3.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                nr3Received.Enqueue(msg);
                _sync.Release(msg);
            });
            var sub3 = _client3.Sub(subInfo3);

            await Context.DelayAsync();

            _client1.Pub(subject, "mess1");
            _sync.WaitForAll();

            _client1.Unsub(sub1);
            _client2.Unsub(sub2);
            _client3.Unsub(sub3);
            await Context.DelayAsync();

            _client1.Pub(subject, "mess2");
            await Context.DelayAsync();

            _sync.InterceptedCount.Should().Be(3);
            nr1Received.Should().HaveCount(1);
            nr2Received.Should().HaveCount(1);
            nr3Received.Should().HaveCount(1);
        }

        [Fact]
        public async Task Client_Should_be_able_to_unsub_from_a_subject_by_disposing_a_subscription_object()
        {
            var subject = Context.GenerateSubject();
            var nr1Received = new ConcurrentQueue<MsgOp>();
            var nr2Received = new ConcurrentQueue<MsgOp>();
            var nr3Received = new ConcurrentQueue<MsgOp>();
            var subInfo1 = new SubscriptionInfo(subject);
            var subInfo2 = new SubscriptionInfo(subject);
            var subInfo3 = new SubscriptionInfo(subject);

            _sync = Sync.MaxThree();
            await ConnectAllClients();

            _client1.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                nr1Received.Enqueue(msg);
                _sync.Release(msg);
            });
            var sub1 = _client1.Sub(subInfo1);

            _client2.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                nr2Received.Enqueue(msg);
                _sync.Release(msg);
            });
            var sub2 = _client2.Sub(subInfo2);

            _client3.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                nr3Received.Enqueue(msg);
                _sync.Release(msg);
            });
            var sub3 = _client3.Sub(subInfo3);

            await Context.DelayAsync();

            _client1.Pub(subject, "mess1");
            _sync.WaitForAll();

            sub1.Dispose();
            sub2.Dispose();
            sub3.Dispose();
            await Context.DelayAsync();

            _client1.Pub(subject, "mess2");
            await Context.DelayAsync();

            _sync.InterceptedCount.Should().Be(3);
            nr1Received.Should().HaveCount(1);
            nr2Received.Should().HaveCount(1);
            nr3Received.Should().HaveCount(1);
        }

        [Fact]
        public async Task Client_Should_be_able_to_auto_unsub_after_n_messages_to_subject()
        {
            var subject = Context.GenerateSubject();
            var nr2Received = new ConcurrentQueue<MsgOp>();
            var nr3Received = new ConcurrentQueue<MsgOp>();
            var subInfo2 = new SubscriptionInfo(subject, maxMessages: 2);
            var subInfo3 = new SubscriptionInfo(subject, maxMessages: 2);

            _sync = Sync.MaxTwo();
            await ConnectAllClients();

            _client2.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                nr2Received.Enqueue(msg);
                _sync.Release(msg);
            });
            _client2.Sub(subInfo2);

            _client3.OpStream.OfType<MsgOp>().Subscribe(msg =>
            {
                nr3Received.Enqueue(msg);
                _sync.Release(msg);
            });
            _client3.Sub(subInfo3);

            await Context.DelayAsync();

            _client1.Pub(subject, "mess1");
            _sync.WaitForAll();

            _client1.Pub(subject, "mess2");
            _sync.WaitForAll();

            _client1.Pub(subject, "mess3");
            await Context.DelayAsync();

            _sync.InterceptedCount.Should().Be(4);
            nr2Received.Should().HaveCount(2);
            nr3Received.Should().HaveCount(2);
        }
    }
}