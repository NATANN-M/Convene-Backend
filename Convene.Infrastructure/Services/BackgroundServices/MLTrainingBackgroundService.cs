using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Convene.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Convene.Infrastructure.BackgroundServices
{
    public class MLTrainingBackgroundService : BackgroundService
    {
        private readonly ILogger<MLTrainingBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval = TimeSpan.FromHours(1);

        public MLTrainingBackgroundService(
            ILogger<MLTrainingBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ML background training service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Running automatic ML training...");

                    // Create a scope for scoped services
                    using var scope = _serviceProvider.CreateScope();
                    var mlService = scope.ServiceProvider.GetRequiredService<IMLService>();

                    var trained = await mlService.TrainModelAsync();

                    if (trained)
                        _logger.LogInformation("ML model successfully trained at {time}.", DateTime.UtcNow);
                    else
                        _logger.LogWarning("ML training skipped (not enough data) at {time}.", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ML auto-training failed.");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
