using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient.Ops;
using Xunit;

namespace MyNatsClient.IntegrationTests
{
    public class ConsumerTests : ClientIntegrationTests
    {
        private NatsClient _client;
        private NatsConsumer _consumer;

        public ConsumerTests()
        {
            _client = new NatsClient("tc1", ConnectionInfo);
            _consumer = new NatsConsumer(_client);
            _client.Connect();
        }

        protected override void OnAfterEachTest()
        {
            _consumer?.Dispose();
            _consumer = null;

            _client?.Disconnect();
            _client?.Dispose();
            _client = null;
        }

        [Fact]
        public async Task Should_get_only_subject_specific_messages_When_subscribing_synchronously_using_observer()
        {
            const string subject = "64c5822e794a43b0b71222e0d4942b64";
            const string otherSubject = subject + "fail";
            var interceptedSubjects = new List<string>();

            _client.Sub(otherSubject, "subid1");

            var observer = new DelegatingObserver<MsgOp>(msg =>
            {
                interceptedSubjects.Add(msg.Subject);
                ReleaseOne();
            });
            using (_consumer.Subscribe(subject, observer))
            {
                await _client.PubAsync(subject, "Test1");
                WaitOne();
                await _client.PubAsync(subject, "Test2");
                WaitOne();
                await _client.PubAsync(otherSubject, "Test3");
                WaitOne();
            }

            interceptedSubjects.Should().HaveCount(2);
            interceptedSubjects.Should().OnlyContain(i => i == subject);
        }

        [Fact]
        public async Task Should_get_only_subject_specific_messages_When_subscribing_asynchronously_using_observer()
        {
            const string subject = "64c5822e794a43b0b71222e0d4942b64";
            const string otherSubject = subject + "fail";
            var interceptedSubjects = new List<string>();

            _client.Sub(otherSubject, "subid1");

            var observer = new DelegatingObserver<MsgOp>(msg =>
            {
                interceptedSubjects.Add(msg.Subject);
                ReleaseOne();
            });
            using (await _consumer.SubscribeAsync(subject, observer))
            {
                await _client.PubAsync(subject, "Test1");
                WaitOne();
                await _client.PubAsync(subject, "Test2");
                WaitOne();
                await _client.PubAsync(otherSubject, "Test3");
                WaitOne();
            }

            interceptedSubjects.Should().HaveCount(2);
            interceptedSubjects.Should().OnlyContain(i => i == subject);
        }

        [Fact]
        public async Task Should_get_only_subject_specific_messages_When_subscribing_synchronously_using_handler()
        {
            const string subject = "64c5822e794a43b0b71222e0d4942b64";
            const string otherSubject = subject + "fail";
            var interceptedSubjects = new List<string>();

            _client.Sub(otherSubject, "subid1");

            Action<MsgOp> handler = msg =>
            {
                interceptedSubjects.Add(msg.Subject);
                ReleaseOne();
            };
            using (_consumer.Subscribe(subject, handler))
            {
                await _client.PubAsync(subject, "Test1");
                WaitOne();
                await _client.PubAsync(subject, "Test2");
                WaitOne();
                await _client.PubAsync(otherSubject, "Test3");
                WaitOne();
            }

            interceptedSubjects.Should().HaveCount(2);
            interceptedSubjects.Should().OnlyContain(i => i == subject);
        }

        [Fact]
        public async Task Should_get_only_subject_specific_messages_When_subscribing_asynchronously_using_handler()
        {
            const string subject = "64c5822e794a43b0b71222e0d4942b64";
            const string otherSubject = subject + "fail";
            var interceptedSubjects = new List<string>();

            _client.Sub(otherSubject, "subid1");

            Action<MsgOp> handler = msg =>
            {
                interceptedSubjects.Add(msg.Subject);
                ReleaseOne();
            };
            using (await _consumer.SubscribeAsync(subject, handler))
            {
                await _client.PubAsync(subject, "Test1");
                WaitOne();
                await _client.PubAsync(subject, "Test2");
                WaitOne();
                await _client.PubAsync(otherSubject, "Test3");
                WaitOne();
            }

            interceptedSubjects.Should().HaveCount(2);
            interceptedSubjects.Should().OnlyContain(i => i == subject);
        }

        [Fact]
        public async Task Should_not_get_messages_When_the_subscription_has_been_disposed()
        {
            const string subject = "e6f12d099ec34fdba0e43b111dfb95f6";
            var interceptCount = 0;

            var observer = new DelegatingObserver<MsgOp>(msg =>
            {
                Interlocked.Increment(ref interceptCount);
                ReleaseOne();
            });
            using (_consumer.Subscribe(subject, observer))
            {
                await _client.PubAsync(subject, "Test1");
                WaitOne();
                await _client.PubAsync(subject, "Test2");
                WaitOne();
            }

            await _client.PubAsync(subject, "Test3");
            WaitOne();

            interceptCount.Should().Be(2);
        }

        [Fact]
        public async Task Should_not_get_messages_When_the_subscription_has_been_unsubscribed_synchronously()
        {
            const string subject = "e6f12d099ec34fdba0e43b111dfb95f6";
            var interceptCount = 0;

            var observer = new DelegatingObserver<MsgOp>(msg =>
            {
                Interlocked.Increment(ref interceptCount);
                ReleaseOne();
            });

            using (var subscription = _consumer.Subscribe(subject, observer))
            {
                await _client.PubAsync(subject, "Test1");
                WaitOne();
                await _client.PubAsync(subject, "Test2");
                WaitOne();

                _consumer.Unsubscribe(subscription);

                await _client.PubAsync(subject, "Test3");
                WaitOne();
            }

            interceptCount.Should().Be(2);
        }

        [Fact]
        public async Task Should_not_get_messages_When_the_subscription_has_been_unsubscribed_asynchronously()
        {
            const string subject = "e6f12d099ec34fdba0e43b111dfb95f6";
            var interceptCount = 0;

            var observer = new DelegatingObserver<MsgOp>(msg =>
            {
                Interlocked.Increment(ref interceptCount);
                ReleaseOne();
            });

            using (var subscription = _consumer.Subscribe(subject, observer))
            {
                await _client.PubAsync(subject, "Test1");
                WaitOne();
                await _client.PubAsync(subject, "Test2");
                WaitOne();

                await _consumer.UnsubscribeAsync(subscription);

                await _client.PubAsync(subject, "Test3");
                WaitOne();
            }

            interceptCount.Should().Be(2);
        }

        [Fact]
        public async Task Should_resubscribe_When_client_reconnects()
        {
            const string subject = "4f90a7dd4971430fbf5151a1116c9cfc";
            var interceptCount = 0;

            var observer = new DelegatingObserver<MsgOp>(msg =>
            {
                Interlocked.Increment(ref interceptCount);
                ReleaseOne();
            });
            using (_consumer.Subscribe(subject, observer))
            {
                await _client.PubAsync(subject, "Test1");
                WaitOne();
                _client.Disconnect();

                _client.Connect();
                await _client.PubAsync(subject, "Test2");
                WaitOne();
            }

            interceptCount.Should().Be(2);
        }

        [Fact]
        public async Task Should_unsub_handler_and_client_from_broker_When_consumer_is_disposed()
        {
            const string subject = "4f90a7dd4971430fbf5151a1116c9cfc";
            var interceptCount = 0;

            var observer = new DelegatingObserver<MsgOp>(msg =>
            {
                Interlocked.Increment(ref interceptCount);
                ReleaseOne();
            });
            _consumer.Subscribe(subject, observer);

            await _client.PubAsync(subject, "Test1");
            WaitOne();

            _consumer.Dispose();
            _consumer = null;

            _client.MsgOpStream.Where(m => m.Subject == subject).Subscribe(observer);

            await _client.PubAsync(subject, "Test2");
            WaitOne();

            interceptCount.Should().Be(1);
        }

        [Fact]
        public async Task Should_be_able_to_subscribe_using_wildcard_When_subscribing_synchronously_using_handler()
        {
            const string subjectNs = "foo.tests.";
            var interceptedSubjects = new List<string>();

            Action<MsgOp> handler = msg =>
            {
                interceptedSubjects.Add(msg.Subject);
                ReleaseOne();
            };
            using (_consumer.Subscribe(subjectNs + "*", handler))
            {
                await _client.PubAsync(subjectNs + "type1", "Test1");
                WaitOne();
                await _client.PubAsync(subjectNs + "type2", "Test2");
                WaitOne();
                await _client.PubAsync(subjectNs + "type3", "Test3");
                WaitOne();
            }

            interceptedSubjects.Should().HaveCount(3);
            interceptedSubjects.Should().OnlyContain(i => i.StartsWith(subjectNs));
        }
    }
}