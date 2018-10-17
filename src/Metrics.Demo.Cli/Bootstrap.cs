using System;
using System.Linq;
using System.Net;
using System.Threading;
using DatadogSharp.DogStatsd;
using Serilog;
using Serilog.Context;

namespace Metrics.Demo.Cli
{
    internal class Bootstrap
    {
        public static void Statsd(string appName, Serilog.Core.Logger logger)
        {
            var serverName = Environment.GetEnvironmentVariable("STATSD_SERVER");
            if (string.IsNullOrEmpty(serverName))
            {
                serverName = "localhost";
            }

            var defaultTags = new [] {
                "host:" + Environment.MachineName.ToLower(),
                "environment:" + Environment.GetEnvironmentVariable("ENVIRONMENT")
            };

            IPHostEntry hostEntry = null;
            var retryCount = 3;

            while(retryCount-- > 0)
            {
                try
                {
                    hostEntry = System.Net.Dns.GetHostEntry(serverName);
                    
                    foreach(var address in hostEntry.AddressList)
                    {
                        logger.Information("Found address {address}, {addr_v4}, {addr_v6}", address, address.MapToIPv4(), address.MapToIPv6());
                    }

                    break;
                }
                catch(Exception ex)
                {
                    logger.Warning(ex, "Failed to resolve {server_name}", serverName);
                    Thread.Sleep(1000);
                }
            }
            if(retryCount <= 0)
            {
                logger.Error("Unable to resolve address of {server_name}", serverName);
                Environment.Exit(1);
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

        public static Serilog.Core.Logger Logger(string appName)
        {
            var logger = new LoggerConfiguration()
                            .Enrich.FromLogContext()
                            .WriteTo.Console(formatter: new Serilog.Formatting.Compact.CompactJsonFormatter())
                            .CreateLogger();

            LogContext.PushProperty("application", appName);
            
            return logger;
        }
    }
}