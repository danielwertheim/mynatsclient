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

            builder
                .AddJsonFile("integrationtests.json", false, false)
                .AddJsonFile("integrationtests.local.json", true, false)
                .AddEnvironmentVariables("MyNats_");

            Config = builder.Build();
        }

        public static Credentials GetCredentials()
        {
            var credentialsConfig = Config.GetSection("credentials");
            if (!credentialsConfig.Exists())
                throw new Exception("Test configuration is missing 'credentials' section.");

            return new Credentials(credentialsConfig["user"], credentialsConfig["pass"]);
        }

        public static Host[] GetHosts(string context)
        {
            if (string.IsNullOrWhiteSpace(context))
                throw new ArgumentException("Context must be provided.", nameof(context));

            return Config
                .GetSection($"{context}:hosts")
                .GetChildren()
                .Select(i => new Host(i["address"], int.Parse(i["port"])))
                .ToArray();
        }
    }
}