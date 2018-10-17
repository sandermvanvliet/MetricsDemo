using System;
using System.Threading;
using System.Threading.Tasks;
using DatadogSharp.DogStatsd;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Metrics.Demo.Cli
{
    public class MetricPusherService : IHostedService
    {
        private readonly ILogger logger;
        private Timer _timer;

        public MetricPusherService(ILogger logger)
        {
            this.logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.Information("Starting metric pusher");

            var random = new Random();
            var delay = random.Next(250, 1000);

            _timer = new Timer(GenerateMetric, null, TimeSpan.FromMilliseconds(delay), TimeSpan.FromSeconds(1));
            
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.Information("Stopping metric pusher");

            _timer.Change(Timeout.InfiniteTimeSpan, TimeSpan.Zero);
            _timer.Dispose();
            _timer = null;

            return Task.CompletedTask;
        }

        private void GenerateMetric(object state)
        {
            var random = new Random();
            var loopValue = random.Next(10, 100);
            var tagValue = random.Next(1, 100) % 2 == 0 ? "red" : "blue";
            
            var tags = new [] { 
                "type:" + tagValue,
                "success:" + (random.Next(1, 100) % 2 == 0).ToString().ToLower()
            };

            logger.Information("Pushing metric with loop value {loop_value}", loopValue);
            
            DatadogStats.Default.Increment("number_of_loops");
            DatadogStats.Default.Gauge("loop_value", loopValue, tags: tags);
            DatadogStats.Default.Timer("other_api.get_data.duration", random.Next(250, 500), tags: tags);
            DatadogStats.Default.Timer("other_api.push_data.duration", random.Next(250, 500), tags: tags);
        }
    }
}