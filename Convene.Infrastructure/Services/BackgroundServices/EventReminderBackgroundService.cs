using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Convene.Application.Interfaces;
using Convene.Domain.Enums;
using Convene.Infrastructure.Persistence;

public class EventReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventReminderBackgroundService> _logger;

    public EventReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<EventReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EventReminderBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing event reminders");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }

        _logger.LogInformation("EventReminderBackgroundService stopped");
    }

    private async Task ProcessRemindersAsync()
    {
        _logger.LogInformation("Event reminder job started at {Time}", DateTime.UtcNow);

        int oneDayCount = 0;
        int twoHourCount = 0;
        int startNowCount = 0;

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConveneDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;

        var oneDayFrom = now.AddHours(23.8);
        var oneDayTo = now.AddHours(24.2);

        var twoHourFrom = now.AddHours(1.8);
        var twoHourTo = now.AddHours(2.2);

        var startNowFrom = now.AddMinutes(-5);
        var startNowTo = now.AddMinutes(5);

        var events = await context.Events
            .Include(e => e.Bookings)
            .Where(e => e.Status == EventStatus.Published)
            .ToListAsync();

        foreach (var ev in events)
        {
            foreach (var booking in ev.Bookings
                .Where(b => b.Status == BookingStatus.Confirmed))
            {
                // 1 Day Before
                if (ev.StartDate >= oneDayFrom && ev.StartDate <= oneDayTo)
                {
                    await notificationService.SendNotificationWithReferenceAsync(
                        booking.UserId,
                        "Event Tomorrow",
                        $"Your event \"{ev.Title}\" will start tomorrow.",
                        NotificationType.EventReminderOneDay,
                        $"EVENT:{ev.Id}:ONE_DAY"
                    );

                    oneDayCount++;
                }

                // 2 Hours Before
                if (ev.StartDate >= twoHourFrom && ev.StartDate <= twoHourTo)
                {
                    await notificationService.SendNotificationWithReferenceAsync(
                        booking.UserId,
                        "Event in 2 Hours",
                        $"Your event \"{ev.Title}\" will start in 2 hours.",
                        NotificationType.EventReminderTwoHours,
                        $"EVENT:{ev.Id}:TWO_HOURS"
                    );

                    twoHourCount++;
                }

                // Starting Now
                if (ev.StartDate >= startNowFrom && ev.StartDate <= startNowTo)
                {
                    await notificationService.SendNotificationWithReferenceAsync(
                        booking.UserId,
                        "Event Started",
                        $"\"{ev.Title}\" is starting now.",
                        NotificationType.EventStartingNow,
                        $"EVENT:{ev.Id}:START_NOW"
                    );

                    startNowCount++;
                }
            }
        }

        _logger.LogInformation(
            "Event reminder job finished. OneDay={OneDay}, TwoHours={TwoHours}, StartNow={StartNow}",
            oneDayCount,
            twoHourCount,
            startNowCount
        );
    }
}
