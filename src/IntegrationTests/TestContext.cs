using System;
using System.Threading.Tasks;
using MyNatsClient;
using Xunit;

namespace IntegrationTests
{
    public abstract class Tests<TContext> : IClassFixture<TContext> where TContext : TestContext
    {
        protected TContext Context { get; }

        protected Tests(TContext context)
        {
            Context = context;
        }
    }

    public abstract class TestContext
    {
        private readonly Host[] _hosts;

        protected TestContext(Host[] hosts)
        {
            LoggerManager.UseNullLogger();

            _hosts = hosts;
        }

        public async Task DelayAsync()
            => await Task.Delay(250);

        public ConnectionInfo GetConnectionInfo()
            => new ConnectionInfo(_hosts)
            {
                AutoRespondToPing = false,
                Verbose = false
            };

        public NatsClient CreateClient(ConnectionInfo connectionInfo = null)
            => new NatsClient(connectionInfo ?? GetConnectionInfo());

        public async Task<NatsClient> ConnectClientAsync(ConnectionInfo connectionInfo = null)
        {
            var client = CreateClient(connectionInfo);

            await client.ConnectAsync();

            return client;
        }

        public string GenerateSubject()
            => Guid.NewGuid().ToString("N");
    }

    public sealed class DefaultContext : TestContext
    {
        public const string Name = nameof(DefaultContext);

        public DefaultContext()
            : base(TestSettings.GetHosts(Name))
        {
        }
    }

    public sealed class BasicAuthContext : TestContext
    {
        public const string Name = nameof(BasicAuthContext);

        public BasicAuthContext()
            : base(TestSettings.GetHosts(Name))
        {}
    }
}