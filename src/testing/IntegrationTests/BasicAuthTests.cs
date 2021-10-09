using System;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient;
using Xunit;

namespace IntegrationTests
{
    public class BasicAuthTests : Tests<BasicAuthContext>
    {
        public BasicAuthTests(BasicAuthContext context)
            : base(context)
        { }

        [Fact]
        public async Task Client_Should_be_able_to_connect_When_valid_credentials_are_used()
        {
            using var client = Context.CreateClient();

            await client.ConnectAsync();

            client.IsConnected.Should().BeTrue();
        }

        [Fact]
        public async Task Client_Should_not_be_able_to_connect_When_empty_credentials_are_used()
        {
            var cnInfo = Context.GetConnectionInfo();
            cnInfo.Credentials = Credentials.Empty;
            cnInfo.Hosts[0].Credentials = Credentials.Empty;

            Func<Task> a = async () =>
            {
                using var client = Context.CreateClient(cnInfo);
                await client.ConnectAsync();
            };

            (await FluentActions.Invoking(async () =>
            {
                using var client = Context.CreateClient(cnInfo);
                await client.ConnectAsync();
            }).Should().ThrowAsync<NatsException>()).And.ExceptionCode.Should().Be(NatsExceptionCodes.MissingCredentials);
        }

        [Fact]
        public async Task Client_Should_not_be_able_to_connect_When_invalid_credentials_are_used()
        {
            var invalidCredentials = new Credentials("wrong", "credentials");
            var cnInfo = Context.GetConnectionInfo();
            cnInfo.Credentials = invalidCredentials;
            cnInfo.Hosts[0].Credentials = invalidCredentials;

            (await FluentActions.Invoking(async () =>
            {
                using var client = Context.CreateClient(cnInfo);
                await client.ConnectAsync();
            }).Should().ThrowAsync<NatsException>()).And.ExceptionCode.Should().Be(NatsExceptionCodes.FailedToConnectToHost);
        }
    }
}
