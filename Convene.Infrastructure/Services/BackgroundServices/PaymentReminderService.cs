using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Convene.Application.EmailTemplates;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Domain.Enums;
using Convene.Infrastructure.Persistence;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Convene.Infrastructure.BackgroundServices
{
    public class PaymentReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaymentReminderService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30); // run every 30 min

        public PaymentReminderService(IServiceProvider serviceProvider, ILogger<PaymentReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Payment Reminder Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendPendingPaymentRemindersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, " Error in PaymentReminderService.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
          }
        }

        private async Task SendPendingPaymentRemindersAsync(CancellationToken token)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ConveneDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            // Get unpaid payments older than 30 min and no reminder sent
            var threshold = DateTime.UtcNow.AddMinutes(-30);

            var unpaidPayments = await dbContext.Payments
                .AsNoTracking() 
                .Include(p => p.Booking)  
                    .ThenInclude(b => b.Event)  
                .Include(p => p.Booking)  
                    .ThenInclude(b => b.User)  
                .Where(p => p.Status == PaymentStatus.Pending
                            && !p.ReminderSent
                            && p.CreatedAt <= threshold)
                .Select(p => new 
                {
                    Payment = p,
                    UserId = p.Booking.UserId,
                    EventTitle = p.Booking.Event.Title,
                    UserEmail = p.Booking.User.Email ,
                    CheckOutUrl=p.ChapaCheckoutUrl
                })
                .ToListAsync(token);

            if (!unpaidPayments.Any())
            {
                _logger.LogInformation("No pending payments requiring reminders.");
                return;
            }

            _logger.LogInformation($"Found {unpaidPayments.Count} payments requiring reminders.");

            foreach (var item in unpaidPayments)
            {
                var payment = item.Payment;

                try
                {
                    // Send email
                    string subject = "? Reminder: Complete Your Event Payment";

                    //string body = $@"
                    //    <h3>Hi {payment.PayerName},</h3>
                    //    <p>This is a friendly reminder that your payment for the event <strong>{item.EventTitle}</strong> is still pending.</p>
                    //    <p>Please complete your payment soon to confirm your booking.</p>
                    //    <a href='{payment.ChapaCheckoutUrl}' style='color:white;background:#007bff;padding:10px 15px;border-radius:5px;text-decoration:none;'>Complete Payment</a>
                    //    <p>Thank you,<br/>Convene Team</p>";


                    var htmlbody = PaymentReminderAndFailedToPayEmailTemplete.GetPaymentReminderHtml(payment.PayerName, item.EventTitle, payment.PaymentReference);

                    await emailService.SendEmailAsync(payment.PayerEmail, subject, htmlbody);

                    await notificationService.SendNotificationAsync(
                        item.UserId,
                        "Payment Reminder",
                        $"Your payment for booking {item.EventTitle} is still pending. Please complete it soon.",
                        NotificationType.PaymentReminder
                    );

                    // Update reminder flag efficiently without tracking
                    var paymentToUpdate = new Payment
                    {
                        Id = payment.Id,
                        ReminderSent = true,
                        ReminderSentAt = DateTime.UtcNow
                    };

                    dbContext.Payments.Attach(paymentToUpdate);
                    dbContext.Entry(paymentToUpdate).Property(p => p.ReminderSent).IsModified = true;
                    dbContext.Entry(paymentToUpdate).Property(p => p.ReminderSentAt).IsModified = true;

                    await dbContext.SaveChangesAsync(token);

                    _logger.LogInformation($"?? Reminder sent to {payment.PayerEmail} for event {item.EventTitle}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error sending reminder for payment {payment.Id}");
                }
            }
        }
    }
}
