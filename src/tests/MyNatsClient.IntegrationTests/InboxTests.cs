using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace MyNatsClient.IntegrationTests
{
    public class InboxTests : ClientIntegrationTests
    {
        private NatsClient _client;

        public InboxTests()
        {
            _client = new NatsClient("tc1", ConnectionInfo);
            _client.Connect();
        }

        protected override void OnAfterEachTest()
        {
            _client?.Disconnect();
            _client?.Dispose();
            _client = null;
        }

        [Fact]
        public async Task Should_get_only_inbox_subject_specific_messages_When_client_is_subscribed_to_other_subject_as_well()
        {
            const string inboxSubject = "64c5822e794a43b0b71222e0d4942b64";
            const string nonInboxSubject = inboxSubject + "fail";
            var interceptedInboxSubjects = new List<string>();

            _client.Sub(nonInboxSubject, "subid1");

            using (_client.CreateInbox(inboxSubject,
                msg =>
                {
                    interceptedInboxSubjects.Add(inboxSubject);
                    ReleaseOne();
                }))
            {
                await _client.PubAsync(inboxSubject, "Test1");
                WaitOne();
                await _client.PubAsync(inboxSubject, "Test2");
                WaitOne();
                await _client.PubAsync(inboxSubject + "fail", "Test2");
                WaitOne();
            }

            interceptedInboxSubjects.Should().HaveCount(2);
            interceptedInboxSubjects.Should().OnlyContain(i => i == inboxSubject);
        }

        [Fact]
        public async Task Should_not_get_messages_When_disposed_the_inbox_subscription()
        {
            const string inboxSubject = "e6f12d099ec34fdba0e43b111dfb95f6";
            var interceptCount = 0;

            using (_client.CreateInbox(inboxSubject, msg =>
            {
                Interlocked.Increment(ref interceptCount);
                ReleaseOne();
            }))
            {
                await _client.PubAsync(inboxSubject, "Test1");
                WaitOne();
                await _client.PubAsync(inboxSubject, "Test2");
                WaitOne();
            }

            await _client.PubAsync(inboxSubject, "Test3");
            WaitOne();

            interceptCount.Should().Be(2);
        }
    }
}