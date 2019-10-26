using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient;
using MyNatsClient.Ops;
using MyNatsClient.Rx;
using Xunit;

namespace IntegrationTests
{
    public class SubTests : Tests<DefaultContext>, IDisposable
    {
        private NatsClient _client;
        private Sync _sync;

        public SubTests(DefaultContext context)
            : base(context)
        {
        }

        public void Dispose()
        {
            _sync?.Dispose();
            _sync = null;

            _client?.Disconnect();
            _client?.Dispose();
            _client = null;
        }

        [Fact]
        public async Task Given_subscribed_sync_using_observer_and_subscribed_to_other_subject_as_well_It_should_only_get_subject_specific_messages()
        {
            var subject = Context.GenerateSubject();
            var otherSubject = subject + "fail";
            var interceptedSubjects = new List<string>();
            var interceptedOtherSubjects = new List<string>();

            _sync = Sync.MaxTwo();
            _client = await Context.ConnectClientAsync();
            _client.Sub(subject, stream => stream.Subscribe(msg =>
            {
                interceptedSubjects.Add(msg.Subject);
                _sync.Release();
            }));
            _client.Sub(otherSubject, stream => stream.Subscribe(msg =>
            {
                interceptedOtherSubjects.Add(msg.Subject);
                _sync.Release();
            }));

            await _client.PubAsync(subject, "Test1");
            _sync.WaitForAny();
            await _client.PubAsync(subject, "Test2");
            _sync.WaitForAny();
            await _client.PubAsync(otherSubject, "Test3");
            _sync.WaitForAny();

            interceptedSubjects.Should().HaveCount(2);
            interceptedSubjects.Should().OnlyContain(i => i == subject);
            interceptedOtherSubjects.Should().HaveCount(1);
            interceptedOtherSubjects.Should().OnlyContain(i => i == otherSubject);
        }

        [Fact]
        public async Task Given_subscribed_async_using_observer_and_subscribed_to_other_subject_as_well_It_should_only_get_subject_specific_messages()
        {
            var subject = Context.GenerateSubject();
            var otherSubject = subject + "fail";
            var interceptedSubjects = new List<string>();
            var interceptedOtherSubjects = new List<string>();

            _sync = Sync.MaxTwo();
            _client = await Context.ConnectClientAsync();
            await _client.SubAsync(subject, stream => stream.Subscribe(msg =>
            {
                interceptedSubjects.Add(msg.Subject);
                _sync.Release();
            }));
            _client.Sub(otherSubject, stream => stream.Subscribe(msg =>
            {
                interceptedOtherSubjects.Add(msg.Subject);
                _sync.Release();
            }));

            await _client.PubAsync(subject, "Test1");
            _sync.WaitForAny();
            await _client.PubAsync(subject, "Test2");
            _sync.WaitForAny();
            await _client.PubAsync(otherSubject, "Test3");
            _sync.WaitForAny();

            interceptedSubjects.Should().HaveCount(2);
            interceptedSubjects.Should().OnlyContain(i => i == subject);
            interceptedOtherSubjects.Should().HaveCount(1);
            interceptedOtherSubjects.Should().OnlyContain(i => i == otherSubject);
        }

        [Fact]
        public async Task Given_subscribed_sync_using_handler_and_subscribed_to_other_subject_as_well_It_should_only_get_subject_specific_messages()
        {
            var subject = Context.GenerateSubject();
            var otherSubject = subject + "fail";
            var interceptedSubjects = new List<string>();
            var interceptedOtherSubjects = new List<string>();

            _sync = Sync.MaxTwo();
            _client = await Context.ConnectClientAsync();
            _client.Sub(subject, stream => stream.Subscribe(msg =>
            {
                interceptedSubjects.Add(msg.Subject);
                _sync.Release();
            }));
            _client.Sub(otherSubject, stream => stream.Subscribe(msg =>
            {
                interceptedOtherSubjects.Add(msg.Subject);
                _sync.Release();
            }));

            await _client.PubAsync(subject, "Test1");
            _sync.WaitForAny();
            await _client.PubAsync(subject, "Test2");
            _sync.WaitForAny();
            await _client.PubAsync(otherSubject, "Test3");
            _sync.WaitForAny();

            interceptedSubjects.Should().HaveCount(2);
            interceptedSubjects.Should().OnlyContain(i => i == subject);
            interceptedOtherSubjects.Should().HaveCount(1);
            interceptedOtherSubjects.Should().OnlyContain(i => i == otherSubject);
        }

        [Fact]
        public async Task Given_subscribed_async_using_handler_and_subscribed_to_other_subject_as_well_It_should_only_get_subject_specific_messages()
        {
            var subject = Context.GenerateSubject();
            var otherSubject = subject + "fail";
            var interceptedSubjects = new List<string>();
            var interceptedOtherSubjects = new List<string>();

            _sync = Sync.MaxTwo();
            _client = await Context.ConnectClientAsync();
            await _client.SubAsync(subject, stream => stream.Subscribe(msg =>
            {
                interceptedSubjects.Add(msg.Subject);
                _sync.Release();
            }));
            _client.Sub(otherSubject, stream => stream.Subscribe(msg =>
            {
                interceptedOtherSubjects.Add(msg.Subject);
                _sync.Release();
            }));

            await _client.PubAsync(subject, "Test1");
            _sync.WaitForAny();
            await _client.PubAsync(subject, "Test2");
            _sync.WaitForAny();
            await _client.PubAsync(otherSubject, "Test3");
            _sync.WaitForAny();

            interceptedSubjects.Should().HaveCount(2);
            interceptedSubjects.Should().OnlyContain(i => i == subject);
            interceptedOtherSubjects.Should().HaveCount(1);
            interceptedOtherSubjects.Should().OnlyContain(i => i == otherSubject);
        }

        [Fact]
        public async Task Given_subscribed_sync_using_observer_When_the_subscription_has_been_disposed_It_should_not_get_messages()
        {
            var subject = Context.GenerateSubject();

            _sync = Sync.MaxOne();
            _client = await Context.ConnectClientAsync();
            var s = _client.Sub(subject, stream => stream.Subscribe(NatsObserver.Delegating<MsgOp>(msg => _sync.Release(msg))));

            await _client.PubAsync(subject, "Test1");
            _sync.WaitForAny();
            await _client.PubAsync(subject, "Test2");
            _sync.WaitForAny();

            s.Dispose();

            await _client.PubAsync(subject, "Test3");
            await Context.DelayAsync();

            _sync.InterceptedCount.Should().Be(2);
        }

        [Fact]
        public async Task Given_subscribed_async_using_observer_When_the_subscription_has_been_disposed_It_should_not_get_messages()
        {
            var subject = Context.GenerateSubject();

            _sync = Sync.MaxOne();
            _client = await Context.ConnectClientAsync();
            var s = await _client.SubAsync(subject, stream => stream.Subscribe(NatsObserver.Delegating<MsgOp>(msg => _sync.Release(msg))));

            await _client.PubAsync(subject, "Test1");
            _sync.WaitForAny();
            await _client.PubAsync(subject, "Test2");
            _sync.WaitForAny();

            s.Dispose();

            await _client.PubAsync(subject, "Test3");
            await Context.DelayAsync();

            _sync.InterceptedCount.Should().Be(2);
        }

        [Fact]
        public async Task Given_subscribed_sync_using_handler_When_the_subscription_has_been_disposed_It_should_not_get_messages()
        {
            var subject = Context.GenerateSubject();

            _sync = Sync.MaxOne();
            _client = await Context.ConnectClientAsync();
            var s = _client.Sub(subject, stream => stream.Subscribe(msg => _sync.Release(msg)));

            await _client.PubAsync(subject, "Test1");
            _sync.WaitForAny();
            await _client.PubAsync(subject, "Test2");
            _sync.WaitForAny();

            s.Dispose();

            await _client.PubAsync(subject, "Test3");
            await Context.DelayAsync();

            _sync.InterceptedCount.Should().Be(2);
        }

        [Fact]
        public async Task Given_subscribed_async_using_handler_When_the_subscription_has_been_disposed_It_should_not_get_messages()
        {
            var subject = Context.GenerateSubject();

            _sync = Sync.MaxOne();
            _client = await Context.ConnectClientAsync();
            var s = await _client.SubAsync(subject, stream => stream.Subscribe(msg => _sync.Release(msg)));

            await _client.PubAsync(subject, "Test1");
            _sync.WaitForAny();
            await _client.PubAsync(subject, "Test2");
            _sync.WaitForAny();

            s.Dispose();

            await _client.PubAsync(subject, "Test3");
            await Context.DelayAsync();

            _sync.InterceptedCount.Should().Be(2);
        }

        [Fact]
        public async Task Given_subscribed_When_unsubscribing_sync_It_should_not_get_any_further_messages()
        {
            var subject = Context.GenerateSubject();

            _sync = Sync.MaxOne();
            _client = await Context.ConnectClientAsync();
            var subscription = _client.Sub(subject, stream => stream.Subscribe(NatsObserver.Delegating<MsgOp>(msg => _sync.Release(msg))));

            await _client.PubAsync(subject, "Test1");
            _sync.WaitForAny();
            await _client.PubAsync(subject, "Test2");
            _sync.WaitForAny();

            await _client.UnsubAsync(subscription.SubscriptionInfo);

            await _client.PubAsync(subject, "Test3");
            await Context.DelayAsync();

            _sync.InterceptedCount.Should().Be(2);
        }

        [Fact]
        public async Task Given_subscribed_When_unsubscribing_async_It_should_not_get_any_further_messages()
        {
            var subject = Context.GenerateSubject();

            _sync = Sync.MaxOne();
            _client = await Context.ConnectClientAsync();
            var s = _client.Sub(subject, stream => stream.Subscribe(NatsObserver.Delegating<MsgOp>(msg => _sync.Release(msg))));

            await _client.PubAsync(subject, "Test1");
            _sync.WaitForAny();
            await _client.PubAsync(subject, "Test2");
            _sync.WaitForAny();

            await _client.UnsubAsync(s.SubscriptionInfo);

            await _client.PubAsync(subject, "Test3");
            await Context.DelayAsync();

            _sync.InterceptedCount.Should().Be(2);
        }

        [Fact]
        public async Task Given_subscribed_When_disconnectiong_and_connecting_again_It_should_resubscribe_and_get_messages()
        {
            var subject = Context.GenerateSubject();

            _sync = Sync.MaxOne();
            _client = await Context.ConnectClientAsync();
            _client.Sub(subject, stream => stream.Subscribe(NatsObserver.Delegating<MsgOp>(msg => _sync.Release(msg))));

            await _client.PubAsync(subject, "Test1");
            _sync.WaitForAny();
            _client.Disconnect();

            await _client.ConnectAsync();
            await _client.PubAsync(subject, "Test2");
            _sync.WaitForAny();

            _sync.InterceptedCount.Should().Be(2);
        }

        [Fact]
        public async Task Given_subscribed_with_wildcard_sync_using_observer_It_should_get_messages()
        {
            const string subjectNs = "foo.tests.";

            _sync = Sync.MaxOne();
            _client = await Context.ConnectClientAsync();
            _client.Sub(subjectNs + "*", stream => stream.Subscribe(NatsObserver.Delegating<MsgOp>(msg => _sync.Release(msg))));

            await _client.PubAsync(subjectNs + "type1", "Test1");
            _sync.WaitForAny();
            await _client.PubAsync(subjectNs + "type2", "Test2");
            _sync.WaitForAny();
            await _client.PubAsync(subjectNs + "type3", "Test3");
            _sync.WaitForAny();

            _sync.InterceptedCount.Should().Be(3);
            _sync.Intercepted.Select(i => i.Subject).Should().OnlyContain(i => i.StartsWith(subjectNs));
        }

        [Fact]
        public async Task Given_subscribed_with_wildcard_async_using_observer_It_should_get_messages()
        {
            const string subjectNs = "foo.tests.";

            _sync = Sync.MaxOne();
            _client = await Context.ConnectClientAsync();
            await _client.SubAsync(subjectNs + "*", stream => stream.Subscribe(NatsObserver.Delegating<MsgOp>(msg => _sync.Release(msg))));

            await _client.PubAsync(subjectNs + "type1", "Test1");
            _sync.WaitForAny();
            await _client.PubAsync(subjectNs + "type2", "Test2");
            _sync.WaitForAny();
            await _client.PubAsync(subjectNs + "type3", "Test3");
            _sync.WaitForAny();

            _sync.InterceptedCount.Should().Be(3);
            _sync.Intercepted.Select(i => i.Subject).Should().OnlyContain(i => i.StartsWith(subjectNs));
        }

        [Fact]
        public async Task Given_subscribed_with_wildcard_sync_using_handler_It_should_get_messages()
        {
            const string subjectNs = "foo.tests.";

            _sync = Sync.MaxOne();
            _client = await Context.ConnectClientAsync();
            _client.Sub(subjectNs + "*", stream => stream.Subscribe(msg => _sync.Release(msg)));

            await _client.PubAsync(subjectNs + "type1", "Test1");
            _sync.WaitForAny();
            await _client.PubAsync(subjectNs + "type2", "Test2");
            _sync.WaitForAny();
            await _client.PubAsync(subjectNs + "type3", "Test3");
            _sync.WaitForAny();

            _sync.InterceptedCount.Should().Be(3);
            _sync.Intercepted.Select(i => i.Subject).Should().OnlyContain(i => i.StartsWith(subjectNs));
        }

        [Fact]
        public async Task Given_subscribed_with_wildcard_async_using_handler_It_should_get_messages()
        {
            const string subjectNs = "foo.tests.";

            _sync = Sync.MaxOne();
            _client = await Context.ConnectClientAsync();
            await _client.SubAsync(subjectNs + "*", stream => stream.Subscribe(msg => _sync.Release(msg)));

            await _client.PubAsync(subjectNs + "type1", "Test1");
            _sync.WaitForAny();
            await _client.PubAsync(subjectNs + "type2", "Test2");
            _sync.WaitForAny();
            await _client.PubAsync(subjectNs + "type3", "Test3");
            _sync.WaitForAny();

            _sync.InterceptedCount.Should().Be(3);
            _sync.Intercepted.Select(i => i.Subject).Should().OnlyContain(i => i.StartsWith(subjectNs));
        }
    }
}