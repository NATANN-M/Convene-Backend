using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Services
{
    public abstract class TrackedBackgroundService : BackgroundService
    {
        private readonly ILogger _logger;

        // Track last successful run
        public DateTime? LastRunTime { get; private set; }
        public string? LastRunResult { get; private set; }

        protected TrackedBackgroundService(ILogger logger)
        {
            _logger = logger;
        }

        protected async Task RunTaskAsync(Func<CancellationToken, Task<string>> executeTask, CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                LastRunTime = DateTime.UtcNow;
                _logger.LogInformation("? {Service} running at {Time}", GetType().Name, LastRunTime);

                try
                {
                    // Execute the actual service logic and get result string
                    LastRunResult = await executeTask(stoppingToken);
                    _logger.LogInformation("? {Service} finished: {Result}", GetType().Name, LastRunResult);
                }
                catch (Exception ex)
                {
                    LastRunResult = "Failed";
                    _logger.LogError(ex, "? {Service} failed.", GetType().Name);
                }

                _logger.LogInformation("?? {Service} sleeping for {Minutes} minutes", GetType().Name, GetDelay().TotalMinutes);
                await Task.Delay(GetDelay(), stoppingToken);
            }
        }

        // Each child service defines its interval
        protected abstract TimeSpan GetDelay();
    }
}
