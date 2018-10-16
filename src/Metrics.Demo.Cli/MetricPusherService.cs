using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;
using StatsdClient;

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
            while (!_tokenSource.Token.IsCancellationRequested)
            {
                Thread.Sleep(1000);

                logger.Information("Pushing metric");
                DogStatsd.Increment("loop.count", tags: new[] { "foo:bar" });
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