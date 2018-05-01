using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient;
using MyNatsClient.Extensions;
using MyNatsClient.Ops;
using Xunit;

namespace IntegrationTests
{
    public class ClientSubTests : ClientIntegrationTests
    {
        private NatsClient _client;

        public ClientSubTests()
        {
            _client = new NatsClient(ConnectionInfo);
            _client.Connect();
        }

        protected override void OnAfterEachTest()
        {
            _client?.Disconnect();
            _client?.Dispose();
            _client = null;
        }

        [Fact]
        public async Task Given_subscribed_sync_using_observer_and_subscribed_to_other_subject_as_well_It_should_only_get_subject_specific_messages()
        {
            var subject = GenerateSubject();
            var otherSubject = subject + "fail";
            var interceptedSubjects = new List<string>();

            _client.Sub(subject, stream => stream.Subscribe(NatsObserver.Delegating<MsgOp>(msg =>
            {
                interceptedSubjects.Add(msg.Subject);
                ReleaseOne();
            })));
            _client.Sub(otherSubject);

            await _client.PubAsync(subject, "Test1");
            WaitOne();
            await _client.PubAsync(subject, "Test2");
            WaitOne();
            await _client.PubAsync(otherSubject, "Test3");
            WaitOne();

            interceptedSubjects.Should().HaveCount(2);
            interceptedSubjects.Should().OnlyContain(i => i == subject);
        }

        [Fact]
        public async Task Given_subscribed_async_using_observer_and_subscribed_to_other_subject_as_well_It_should_only_get_subject_specific_messages()
        {
            var subject = GenerateSubject();
            var otherSubject = subject + "fail";
            var interceptedSubjects = new List<string>();

            await _client.SubAsync(subject, stream => stream.Subscribe(NatsObserver.Delegating<MsgOp>(msg =>
            {
                interceptedSubjects.Add(msg.Subject);
                ReleaseOne();
            })));
            _client.Sub(otherSubject);

            await _client.PubAsync(subject, "Test1");
            WaitOne();
            await _client.PubAsync(subject, "Test2");
            WaitOne();
            await _client.PubAsync(otherSubject, "Test3");
            WaitOne();

            interceptedSubjects.Should().HaveCount(2);
            interceptedSubjects.Should().OnlyContain(i => i == subject);
        }

        [Fact]
        public async Task Given_subscribed_sync_using_handler_and_subscribed_to_other_subject_as_well_It_should_only_get_subject_specific_messages()
        {
            var subject = GenerateSubject();
            var otherSubject = subject + "fail";
            var interceptedSubjects = new List<string>();

            _client.Sub(subject, stream => stream.Subscribe(msg =>
            {
                interceptedSubjects.Add(msg.Subject);
                ReleaseOne();
            }));
            _client.Sub(otherSubject);

            await _client.PubAsync(subject, "Test1");
            WaitOne();
            await _client.PubAsync(subject, "Test2");
            WaitOne();
            await _client.PubAsync(otherSubject, "Test3");
            WaitOne();

            interceptedSubjects.Should().HaveCount(2);
            interceptedSubjects.Should().OnlyContain(i => i == subject);
        }

        [Fact]
        public async Task Given_subscribed_async_using_handler_and_subscribed_to_other_subject_as_well_It_should_only_get_subject_specific_messages()
        {
            var subject = GenerateSubject();
            var otherSubject = subject + "fail";
            var interceptedSubjects = new List<string>();

            await _client.SubAsync(subject, stream => stream.Subscribe(msg =>
            {
                interceptedSubjects.Add(msg.Subject);
                ReleaseOne();
            }));
            _client.Sub(otherSubject);

            await _client.PubAsync(subject, "Test1");
            WaitOne();
            await _client.PubAsync(subject, "Test2");
            WaitOne();
            await _client.PubAsync(otherSubject, "Test3");
            WaitOne();

            interceptedSubjects.Should().HaveCount(2);
            interceptedSubjects.Should().OnlyContain(i => i == subject);
        }

        [Fact]
        public async Task Given_subscribed_sync_using_observer_When_the_subscription_has_been_disposed_It_should_not_get_messages()
        {
            var subject = GenerateSubject();
            var interceptCount = 0;

            var s = _client.Sub(subject, stream => stream.Subscribe(NatsObserver.Delegating<MsgOp>(msg =>
            {
                Interlocked.Increment(ref interceptCount);
                ReleaseOne();
            })));

            await _client.PubAsync(subject, "Test1");
            WaitOne();
            await _client.PubAsync(subject, "Test2");
            WaitOne();

            s.Dispose();

            await _client.PubAsync(subject, "Test3");
            WaitOne();

            interceptCount.Should().Be(2);
        }

        [Fact]
        public async Task Given_subscribed_async_using_observer_When_the_subscription_has_been_disposed_It_should_not_get_messages()
        {
            var subject = GenerateSubject();
            var interceptCount = 0;

            var s = await _client.SubAsync(subject, stream => stream.Subscribe(NatsObserver.Delegating<MsgOp>(msg =>
            {
                Interlocked.Increment(ref interceptCount);
                ReleaseOne();
            })));

            await _client.PubAsync(subject, "Test1");
            WaitOne();
            await _client.PubAsync(subject, "Test2");
            WaitOne();

            s.Dispose();

            await _client.PubAsync(subject, "Test3");
            WaitOne();

            interceptCount.Should().Be(2);
        }

        [Fact]
        public async Task Given_subscribed_sync_using_handler_When_the_subscription_has_been_disposed_It_should_not_get_messages()
        {
            var subject = GenerateSubject();
            var interceptCount = 0;

            var s = _client.Sub(subject, stream => stream.Subscribe(msg =>
            {
                Interlocked.Increment(ref interceptCount);
                ReleaseOne();
            }));

            await _client.PubAsync(subject, "Test1");
            WaitOne();
            await _client.PubAsync(subject, "Test2");
            WaitOne();

            s.Dispose();

            await _client.PubAsync(subject, "Test3");
            WaitOne();

            interceptCount.Should().Be(2);
        }

        [Fact]
        public async Task Given_subscribed_async_using_handler_When_the_subscription_has_been_disposed_It_should_not_get_messages()
        {
            var subject = GenerateSubject();
            var interceptCount = 0;

            var s = await _client.SubAsync(subject, stream => stream.Subscribe(msg =>
            {
                Interlocked.Increment(ref interceptCount);
                ReleaseOne();
            }));

            await _client.PubAsync(subject, "Test1");
            WaitOne();
            await _client.PubAsync(subject, "Test2");
            WaitOne();

            s.Dispose();

            await _client.PubAsync(subject, "Test3");
            WaitOne();

            interceptCount.Should().Be(2);
        }

        [Fact]
        public async Task Given_subscribed_When_unsubscribing_sync_It_should_not_get_any_further_messages()
        {
            var subject = GenerateSubject();
            var interceptCount = 0;

            var subscription = _client.Sub(subject, stream => stream.Subscribe(NatsObserver.Delegating<MsgOp>(msg =>
            {
                Interlocked.Increment(ref interceptCount);
                ReleaseOne();
            })));

            await _client.PubAsync(subject, "Test1");
            WaitOne();
            await _client.PubAsync(subject, "Test2");
            WaitOne();

            await _client.UnsubAsync(subscription.SubscriptionInfo);

            await _client.PubAsync(subject, "Test3");
            WaitOne();

            interceptCount.Should().Be(2);
        }

        [Fact]
        public async Task Given_subscribed_When_unsubscribing_async_It_should_not_get_any_further_messages()
        {
            var subject = GenerateSubject();
            var interceptCount = 0;

            var subscription = _client.Sub(subject, stream => stream.Subscribe(NatsObserver.Delegating<MsgOp>(msg =>
            {
                Interlocked.Increment(ref interceptCount);
                ReleaseOne();
            })));

            await _client.PubAsync(subject, "Test1");
            WaitOne();
            await _client.PubAsync(subject, "Test2");
            WaitOne();

            await _client.UnsubAsync(subscription.SubscriptionInfo);

            await _client.PubAsync(subject, "Test3");
            WaitOne();

            interceptCount.Should().Be(2);
        }

        [Fact]
        public async Task Given_subscribed_When_disconnectiong_and_connecting_again_It_should_resubscribe_and_get_messages()
        {
            var subject = GenerateSubject();
            var interceptCount = 0;

            _client.Sub(subject, stream => stream.Subscribe(NatsObserver.Delegating<MsgOp>(msg =>
            {
                Interlocked.Increment(ref interceptCount);
                ReleaseOne();
            })));

            await _client.PubAsync(subject, "Test1");
            WaitOne();
            _client.Disconnect();

            _client.Connect();
            await _client.PubAsync(subject, "Test2");
            WaitOne();

            interceptCount.Should().Be(2);
        }

        [Fact]
        public async Task Given_subscribed_with_wildcard_sync_using_observer_It_should_get_messages()
        {
            const string subjectNs = "foo.tests.";
            var interceptedSubjects = new List<string>();

            _client.Sub(subjectNs + "*", stream => stream.Subscribe(NatsObserver.Delegating<MsgOp>(msg =>
            {
                interceptedSubjects.Add(msg.Subject);
                ReleaseOne();
            })));

            await _client.PubAsync(subjectNs + "type1", "Test1");
            WaitOne();
            await _client.PubAsync(subjectNs + "type2", "Test2");
            WaitOne();
            await _client.PubAsync(subjectNs + "type3", "Test3");
            WaitOne();

            interceptedSubjects.Should().HaveCount(3);
            interceptedSubjects.Should().OnlyContain(i => i.StartsWith(subjectNs));
        }

        [Fact]
        public async Task Given_subscribed_with_wildcard_async_using_observer_It_should_get_messages()
        {
            const string subjectNs = "foo.tests.";
            var interceptedSubjects = new List<string>();

            await _client.SubAsync(subjectNs + "*", stream => stream.Subscribe(NatsObserver.Delegating<MsgOp>(msg =>
            {
                interceptedSubjects.Add(msg.Subject);
                ReleaseOne();
            })));

            await _client.PubAsync(subjectNs + "type1", "Test1");
            WaitOne();
            await _client.PubAsync(subjectNs + "type2", "Test2");
            WaitOne();
            await _client.PubAsync(subjectNs + "type3", "Test3");
            WaitOne();

            interceptedSubjects.Should().HaveCount(3);
            interceptedSubjects.Should().OnlyContain(i => i.StartsWith(subjectNs));
        }

        [Fact]
        public async Task Given_subscribed_with_wildcard_sync_using_handler_It_should_get_messages()
        {
            const string subjectNs = "foo.tests.";
            var interceptedSubjects = new List<string>();

            _client.Sub(subjectNs + "*", stream => stream.Subscribe(msg =>
            {
                interceptedSubjects.Add(msg.Subject);
                ReleaseOne();
            }));

            await _client.PubAsync(subjectNs + "type1", "Test1");
            WaitOne();
            await _client.PubAsync(subjectNs + "type2", "Test2");
            WaitOne();
            await _client.PubAsync(subjectNs + "type3", "Test3");
            WaitOne();

            interceptedSubjects.Should().HaveCount(3);
            interceptedSubjects.Should().OnlyContain(i => i.StartsWith(subjectNs));
        }

        [Fact]
        public async Task Given_subscribed_with_wildcard_async_using_handler_It_should_get_messages()
        {
            const string subjectNs = "foo.tests.";
            var interceptedSubjects = new List<string>();

            await _client.SubAsync(subjectNs + "*", stream => stream.Subscribe(msg =>
            {
                interceptedSubjects.Add(msg.Subject);
                ReleaseOne();
            }));

            await _client.PubAsync(subjectNs + "type1", "Test1");
            WaitOne();
            await _client.PubAsync(subjectNs + "type2", "Test2");
            WaitOne();
            await _client.PubAsync(subjectNs + "type3", "Test3");
            WaitOne();

            interceptedSubjects.Should().HaveCount(3);
            interceptedSubjects.Should().OnlyContain(i => i.StartsWith(subjectNs));
        }

        private static string GenerateSubject() => Guid.NewGuid().ToString("N");
    }
}