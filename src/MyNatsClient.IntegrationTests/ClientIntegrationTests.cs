using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MyNatsClient.IntegrationTests
{
    [TestFixture]
    public abstract class ClientIntegrationTests
    {
        protected const int MaxTimeMs = 2000;
        protected AutoResetEvent Sync;
        protected ConnectionInfo ConnectionInfo;
        protected async Task DelayAsync() => await Task.Delay(250);

        protected ClientIntegrationTests()
        {
            ConnectionInfo = new ConnectionInfo(new Host("ubuntu01", 4223))
            {
                AutoRespondToPing = false,
                Verbose = false,
                Credentials = new Credentials("test", "p@ssword1234")
            };
        }

        [SetUp]
        protected void Init()
        {
            Sync = new AutoResetEvent(false);
            OnBeforeEachTest();
        }

        protected virtual void OnBeforeEachTest() { }

        [TearDown]
        protected void Clean()
        {
            OnAfterEachTest();

            Sync?.Dispose();
            Sync = null;
        }

        protected virtual void OnAfterEachTest() { }
    }
}