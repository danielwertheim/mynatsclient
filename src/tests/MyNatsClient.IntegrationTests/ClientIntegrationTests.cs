using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MyNatsClient.IntegrationTests
{
    [Collection("ClientIntegrationTests")]
    public abstract class ClientIntegrationTests : IDisposable
    {
        private const int MaxTimeMs = 2000;
        private AutoResetEvent _sync;
        protected ConnectionInfo ConnectionInfo;
        protected async Task DelayAsync() => await Task.Delay(250);

        protected ClientIntegrationTests()
        {
            _sync = new AutoResetEvent(false);

            var hosts = TestSettings.GetHosts();

            ConnectionInfo = new ConnectionInfo(hosts)
            {
                AutoRespondToPing = false,
                Verbose = false
            };
        }

        public void Dispose()
        {
            _sync?.Dispose();
            _sync = null;

            OnAfterEachTest();
        }

        protected virtual void OnAfterEachTest() { }

        protected void ReleaseOne()
        {
            _sync.Set();
        }

        protected void WaitOne()
        {
            _sync.WaitOne(MaxTimeMs);
        }
    }
}