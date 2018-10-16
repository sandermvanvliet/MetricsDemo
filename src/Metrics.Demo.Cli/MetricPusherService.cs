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
        private readonly CancellationTokenSource _tokenSource;
        private Task _pusherTask;

        public MetricPusherService(ILogger logger)
        {
            this.logger = logger;
            _tokenSource = new CancellationTokenSource();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.Information("Starting metric pusher");

            _pusherTask = Task.Factory.StartNew(() => PushMetrics());

            return Task.CompletedTask;
        }

        private void PushMetrics()
        {
            var random = new Random();

            while (!_tokenSource.Token.IsCancellationRequested)
            {
                Thread.Sleep(1000);

                var loopValue = random.Next(10, 100);
                var tagValue = random.Next(1, 100) % 2 == 0 ? "red" : "blue";

                logger.Information("Pushing metric with loop value {loop_value}", loopValue);
                
                DatadogStats.Default.Increment("number_of_loops");
                DatadogStats.Default.Gauge("loop_value", loopValue, tags: new[] {"type:" + tagValue });
            }

            logger.Warning("Cancellation requested");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.Information("Stopping metric pusher");

            _tokenSource.Cancel();

            _pusherTask.Wait();

            return Task.CompletedTask;
        }
    }
}