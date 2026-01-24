using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Convene.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Services
{
    public class QueuedHostedService : BackgroundService
    {
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IServiceScopeFactory _scopeFactory;

        public QueuedHostedService(
            IBackgroundTaskQueue taskQueue,
            IServiceScopeFactory scopeFactory)
        {
            _taskQueue = taskQueue;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Background worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    await workItem(scope.ServiceProvider, stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error executing background task: {ex.Message}");
                }
            }
        }
    }
}
