using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Context;
using DatadogSharp.DogStatsd;
using System.Linq;

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

            LogContext.PushProperty("application", appName);

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

            var defaultTags = new [] {
                "host:" + Environment.MachineName.ToLower()
            };


            var hostEntry = System.Net.Dns.GetHostEntry(serverName);
            
            foreach(var address in hostEntry.AddressList)
            {
                logger.Information("Found address {address}, {addr_v4}, {addr_v6}", address, address.MapToIPv4(), address.MapToIPv6());
            }

            var statsdServerAddress = hostEntry
                .AddressList
                .Where(addr => addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .FirstOrDefault()
                .MapToIPv4()
                .ToString();

            logger.Information("Configuring StatsD for server {server_name}({server_ip}) and prefix {application}", serverName, statsdServerAddress, appName);

            DatadogStats.ConfigureDefault(
                address: statsdServerAddress,
                port: 8125,
                metricNamePrefix: appName,
                defaultTags: defaultTags
            );
        }
    }
}
