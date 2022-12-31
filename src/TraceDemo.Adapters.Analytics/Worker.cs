using Microsoft.AspNetCore.Server.HttpSys;
using System.Diagnostics;
using TraceDemo.Adapters.Analytics.RabbitMq;

namespace TraceDemo.Adapters.Analytics
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ActivitySource _activitySource;

        public Worker(
            ILogger<Worker> logger,
            ActivitySource activitySource
        )
        {
            _logger = logger;
            _activitySource = activitySource;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var rabbitMqConsumer = new RabbitMqConsumer(_activitySource);

            while (!stoppingToken.IsCancellationRequested)
            {
                rabbitMqConsumer.TryStartConsumer((message, eventArgs) =>
                {
                    using var activity = _activitySource.StartActivity($"Process CustomerDto");
                    activity?.SetTag("foo", 1);
                    activity?.SetTag("bar", "Hello, World!");
                    activity?.SetTag("baz", new int[] { 1, 2, 3 });

                    _logger.LogInformation($"Received message: {message}");

                    return Task.FromResult((success: true, RequestQueueMode: false));
                });
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}