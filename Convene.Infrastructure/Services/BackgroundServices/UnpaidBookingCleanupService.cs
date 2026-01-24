using Microsoft.EntityFrameworkCore;
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

public class UnpaidBookingCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UnpaidBookingCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // run every 5 minutes
    private readonly TimeSpan _bookingExpiration = TimeSpan.FromHours(24); // auto-cancel after 24 hours
   

    public UnpaidBookingCleanupService(IServiceScopeFactory scopeFactory, ILogger<UnpaidBookingCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
       
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("UnpaidBookingCleanupService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ConveneDbContext>();
                var bookingService = scope.ServiceProvider.GetRequiredService<ITicketService>();
                var notificationservice=scope.ServiceProvider.GetRequiredService<INotificationService>();

                var now = DateTime.UtcNow;
                var expiredBookings = await context.Bookings
                    .Include(b => b.Tickets)
                    .Include(b => b.Payments)
                    .Include(b=> b.Event)
                    .Where(b => b.Status == BookingStatus.Pending &&
                                (b.Payments.All(p => p.Status != PaymentStatus.Paid) || !b.Payments.Any()) &&
                                b.BookingDate <= now - _bookingExpiration)
                    .ToListAsync(stoppingToken);

                if (expiredBookings.Any())
                {
                    foreach (var booking in expiredBookings)
                    {
                        // Cancel booking
                        booking.Status = BookingStatus.Cancelled;
                       
                        var userid = booking.UserId;
                        var titel=booking.Event.Title;

                        await notificationservice.SendNotificationAsync(booking.UserId, "Booking Cancellation", $"Booking For Event {booking.Event.Title} Canceled Due to Payment not Recived in 24 Hours", NotificationType.BookingCancelled);

                        // Cancel tickets and release Sold count
                        await bookingService.CancelTicketsAsync(booking);



                        

                        _logger.LogInformation($"Cancelled unpaid booking {booking.Id} and released {booking.Tickets.Count} tickets.");
                    }

                    await context.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during unpaid booking cleanup.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}
