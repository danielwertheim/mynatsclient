using System.Collections.Generic;
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
        public async Task Should_get_only_subject_specific_messages_When_client_is_subscribed_to_other_subject_as_well()
        {
            const string subject = "64c5822e794a43b0b71222e0d4942b64";
            const string otherSubject = subject + "fail";
            var interceptedSubjects = new List<string>();

            _client.Sub(otherSubject, "subid1");

            var observer = new DelegatingObserver<MsgOp>(msg =>
            {
                interceptedSubjects.Add(subject);
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
    }
}