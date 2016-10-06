using System.IO;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace MyNatsClient.IntegrationTests
{
    public static class TestSettings
    {
        public static IConfigurationRoot Config { get; set; }

        static TestSettings()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory());

            builder.AddJsonFile("testsettings.json");
            Config = builder.Build();
        }

        public static Host[] GetHosts()
        {
            return Config.GetSection("clientIntegrationTests:hosts").GetChildren().Select(i =>
            {
                var u = i["credentials:usr"];
                var p = i["credentials:pwd"];

                return new Host(i["address"], int.Parse(i["port"]))
                {
                    Credentials = !string.IsNullOrWhiteSpace(u) ? new Credentials(u, p) : Credentials.Empty
                };
            }).ToArray();
        }
    }
}