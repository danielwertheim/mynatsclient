using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using MyNatsClient;

namespace IntegrationTests
{
    public static class TestSettings
    {
        public static IConfigurationRoot Config { get; set; }

        static TestSettings()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory);

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