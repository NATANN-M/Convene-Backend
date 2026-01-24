using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Domain.Enums;
using Convene.Infrastructure.Persistence;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Services
{
    public class FeedbackReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<FeedbackReminderService> _logger;
        private readonly IConfiguration _configuration;
        private readonly TimeSpan _interval;
        private readonly int _maxDaysSinceEvent;

        public FeedbackReminderService(
            IServiceProvider serviceProvider,
            ILogger<FeedbackReminderService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;

            // Configurable intervals from appsettings.json
            _interval = TimeSpan.FromHours(
                _configuration.GetValue<int>("FeedbackReminder:IntervalHours", 1));

            _maxDaysSinceEvent = _configuration.GetValue<int>(
                "FeedbackReminder:MaxDaysSinceEvent", 30);

            _logger.LogInformation("FeedbackReminderService initialized");
            _logger.LogInformation($"Will run every {_interval.TotalHours} hour(s)");
            _logger.LogInformation($"Checking events from last {_maxDaysSinceEvent} days");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("?? FeedbackReminderService started");

            // Wait for application to fully start and migrations to complete
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("? Starting feedback reminder check at {Time}", DateTime.UtcNow);
                    await SendFeedbackReminders(stoppingToken);
                    _logger.LogInformation("? Feedback reminder check completed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "? Error in FeedbackReminderService");
                }

                _logger.LogInformation("?? Sleeping for {Hours} hour(s)", _interval.TotalHours);
                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("?? FeedbackReminderService stopped");
        }

        private async Task SendFeedbackReminders(CancellationToken stoppingToken)
        {
            _logger.LogInformation("=== Starting feedback reminder sweep ===");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ConveneDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var now = DateTime.UtcNow;
            var cutoffDate = now.AddDays(-_maxDaysSinceEvent);

            _logger.LogInformation($"Current UTC time: {now:yyyy-MM-dd HH:mm:ss}");
            _logger.LogInformation($"Checking events from: {cutoffDate:yyyy-MM-dd HH:mm:ss} to {now:yyyy-MM-dd HH:mm:ss}");

            // Find events that ended within the configured timeframe
            var eligibleEvents = await context.Events
                .Where(e => e.EndDate <= now && e.EndDate >= cutoffDate)
                .OrderByDescending(e => e.EndDate)
                .ToListAsync(stoppingToken);

            _logger.LogInformation($"?? Found {eligibleEvents.Count} events (ended in last {_maxDaysSinceEvent} days)");

            int totalRemindersSent = 0;
            int totalEventsProcessed = 0;

            foreach (var ev in eligibleEvents)
            {
                totalEventsProcessed++;
                var daysSinceEvent = (now - ev.EndDate).TotalDays;

                _logger.LogInformation($"\n?? Processing event: {ev.Title}");
                _logger.LogInformation($"   Ended: {ev.EndDate:yyyy-MM-dd HH:mm} ({daysSinceEvent:F1} days ago)");

                // Find bookings that need reminders
                var bookings = await context.Bookings
                    .Where(b => b.EventId == ev.Id
                                && b.Status == BookingStatus.Confirmed
                                && !b.FeedbackReminderSent
                                && !context.EventFeedbacks
                                    .Any(f => f.EventId == ev.Id && f.UserId == b.UserId))
                    .Include(b => b.User)
                    .ToListAsync(stoppingToken);

                _logger.LogInformation($"   Found {bookings.Count} bookings needing reminders");

                if (!bookings.Any())
                    continue;

                int eventRemindersSent = 0;

                foreach (var booking in bookings)
                {
                    try
                    {
                        _logger.LogInformation($"   ?? Sending reminder to: {booking.User?.FullName} ({booking.UserId})");

                        await notificationService.SendNotificationAsync(
                            booking.UserId,
                            "Share Your Feedback",
                            $"Hi {booking.User?.FullName}, how was '{ev.Title}'? We'd love to hear your thoughts!",
                            NotificationType.FeedbackReply
                        );

                        booking.FeedbackReminderSent = true;
                        booking.FeedbackReminderSentAt = now;
                        eventRemindersSent++;
                        totalRemindersSent++;

                        _logger.LogInformation($"   ? Reminder sent successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"   ? Failed to send reminder for booking {booking.Id}");
                        // Don't mark as sent if notification failed
                        booking.FeedbackReminderSent = false;
                    }
                }

                if (eventRemindersSent > 0)
                {
                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation($"   ?? Saved {eventRemindersSent} reminder(s) for this event");
                }
            }

            _logger.LogInformation($"\n=== Feedback reminder sweep completed ===");
            _logger.LogInformation($"Processed: {totalEventsProcessed} events");
            _logger.LogInformation($"Sent: {totalRemindersSent} reminders");
            _logger.LogInformation($"Next run in: {_interval.TotalHours} hour(s)");
        }
    }
}
