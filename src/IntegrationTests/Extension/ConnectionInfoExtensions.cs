using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using MyNatsClient;

namespace IntegrationTests.Extension
{
    internal static class ConnectionInfoExtensions
    {
        private static bool AllowAll(X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors) => true;

        internal static ConnectionInfo AllowAllServerCertificates(this ConnectionInfo connectionInfo)
        {
            connectionInfo.ServerCertificateValidation = AllowAll;

            return connectionInfo;
        }
    }
}
