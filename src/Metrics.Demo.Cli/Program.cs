using System;
using System.Threading;
using System.Threading.Tasks;
using StatsdClient;
using Serilog;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Metrics.Demo.Cli
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var appName = Environment.GetEnvironmentVariable("APP_NAME");
            var logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console(formatter: new Serilog.Formatting.Compact.CompactJsonFormatter())
                .CreateLogger();

            BootstrapStatsd(appName, logger);

            logger.Information($"{appName} starting...");

            var hostBuilder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<ILogger>(logger);
                    services.AddHostedService<MetricPusherService>();
                });

            await hostBuilder.RunConsoleAsync();
        }

        private static void BootstrapStatsd(string appName, Serilog.Core.Logger logger)
        {
            var serverName = Environment.GetEnvironmentVariable("STATSD_SERVER");
            if (string.IsNullOrEmpty(serverName))
            {
                serverName = "localhost";
            }

            logger.Information("Configuring StatsD for server {serverName} and prefix {appName}", serverName, appName);

            var dogstatsdConfig = new StatsdConfig
            {
                StatsdServerName = serverName,
                StatsdPort = 8125, // Optional; default is 8125
                Prefix = appName // Optional; by default no prefix will be prepended
            };

            StatsdClient.DogStatsd.Configure(dogstatsdConfig);
        }
    }
}
