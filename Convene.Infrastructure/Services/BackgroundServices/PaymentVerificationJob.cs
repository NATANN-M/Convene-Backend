using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Convene.Application.Interfaces;
using Convene.Domain.Enums;
using Convene.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class PaymentVerificationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentVerificationJob> _logger;

    private readonly TimeSpan _interval = TimeSpan.FromMinutes(2);
    private readonly int _batchSize = 20;
    private readonly int _maxParallelism = 5;
    private readonly TimeSpan _paymentExpiration = TimeSpan.FromHours(24);

    public PaymentVerificationJob(
        IServiceProvider serviceProvider,
        ILogger<PaymentVerificationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment verification job started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await VerifyPendingPayments(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment verification job error.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task VerifyPendingPayments(CancellationToken token)
    {
        var now = DateTime.UtcNow;

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConveneDbContext>();

        // ==============================
        // 1?? LOAD ONLY IDS (SAFE)
        // ==============================
        var bookingPaymentIds = await context.Payments
            .Where(p =>
                p.Status == PaymentStatus.Pending &&
                p.Booking.Status == BookingStatus.Pending)
            .Select(p => p.Id)
            .ToListAsync(token);

        var batches = bookingPaymentIds
            .Select((id, index) => new { id, index })
            .GroupBy(x => x.index / _batchSize)
            .Select(g => g.Select(x => x.id).ToList())
            .ToList();

        int success = 0;
        int failed = 0;

        foreach (var batch in batches)
        {
            if (token.IsCancellationRequested)
                break;

            // ==============================
            // 2?? SAFE PARALLEL PROCESSING
            // ==============================
            await Parallel.ForEachAsync(
                batch,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = _maxParallelism,
                    CancellationToken = token
                },
                async (paymentId, ct) =>
                {
                    try
                    {
                        using var taskScope = _serviceProvider.CreateScope();
                        var db = taskScope.ServiceProvider.GetRequiredService<ConveneDbContext>();
                        var paymentService = taskScope.ServiceProvider.GetRequiredService<IPaymentService>();

                        var payment = await db.Payments
                            .Include(p => p.Booking)
                            .FirstOrDefaultAsync(p => p.Id == paymentId, ct);

                        if (payment == null)
                            return;

                        // ==============================
                        // 3?? EXPIRE ONLY WHEN TRULY OLD
                        // ==============================
                        if (payment.CreatedAt <= now - _paymentExpiration)
                        {
                            payment.Status = PaymentStatus.Failed;
                            await db.SaveChangesAsync(ct);
                            Interlocked.Increment(ref failed);
                            return;
                        }

                        // ==============================
                        // 4?? VERIFY PAYMENT
                        // ==============================
                        bool verified = await paymentService
                            .VerifyPaymentAsync(payment.PaymentReference);

                        if (verified)
                        {
                            payment.Status = PaymentStatus.Paid;
                            await db.SaveChangesAsync(ct);
                            Interlocked.Increment(ref success);
                        }
                        // ? IMPORTANT:
                        // If NOT verified ? DO NOTHING
                        // Keep status PENDING
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Payment verification failed for PaymentId {PaymentId}",
                            paymentId);
                        // ? Do NOT mark failed here
                    }
                });

            await Task.Delay(200, token);
        }

        // ==============================
        // 5?? LOG SUMMARY
        // ==============================
        _logger.LogInformation(
            "Payment verification completed at {Time}. Success: {Success}, Failed: {Failed}, Pending: {Pending}",
            now,
            success,
            failed,
            bookingPaymentIds.Count - success - failed);
    }
}
