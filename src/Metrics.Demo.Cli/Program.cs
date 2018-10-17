using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Context;
using DatadogSharp.DogStatsd;
using System.Linq;
using System.Net;

namespace Metrics.Demo.Cli
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var appName = Environment.GetEnvironmentVariable("APP_NAME");

            var logger = Bootstrap.Logger(appName);

            Bootstrap.Statsd(appName, logger);

            logger.Information($"{appName} starting...");

            var hostBuilder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<ILogger>(logger);
                    services.AddHostedService<MetricPusherService>();
                });

            await hostBuilder.RunConsoleAsync();
        }
    }
}
