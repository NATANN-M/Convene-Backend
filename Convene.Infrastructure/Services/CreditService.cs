using Microsoft.EntityFrameworkCore;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Convene.Application.DTOs.AdminPlatformSetting;
using Convene.Domain.Enums;
using Convene.Application.DTOs.Credits;
using Microsoft.Extensions.DependencyInjection;

namespace Convene.Infrastructure.Services
{
    public class CreditService : ICreditService
    {
        private readonly ConveneDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;

        public CreditService(ConveneDbContext context,
            INotificationService notificationService,
            IBackgroundTaskQueue backgroundTaskQueue)
        {
            _context = context;
            _notificationService = notificationService;
            _backgroundTaskQueue = backgroundTaskQueue;
        }

      
        //  CREATE PENDING CREDIT TRANSACTION 
        public async Task<CreditTransaction> CreatePendingTransactionAsync(
          Guid userId,   // USER ID from JWT
          int creditsToBuy)
        {
            // Get organizer profile
            var organizerProfile = await _context.OrganizerProfiles
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (organizerProfile == null)
                throw new InvalidOperationException("User is not an organizer or Not Found.");

            //  Get credit price
            var settings = await GetPlatformSettingsAsync();
            var totalAmount = creditsToBuy * settings.CreditPriceETB;

            //  Create transaction ONLY (no payment info here)
            var tx = new CreditTransaction
            {
                Id = Guid.NewGuid(),
                OrganizerProfileId = organizerProfile.Id,
                UserId= userId,
                CreditsChanged = creditsToBuy,
                Type = "Purchase",
                Description = $"Purchase {creditsToBuy} credits",
                Status = PaymentStatus.Pending,
                TotalAmount = totalAmount,
                CreatedAt = DateTime.UtcNow
            };

            _context.CreditTransactions.Add(tx);
            await _context.SaveChangesAsync();

            return tx;
        }



       //mark transaction paid and add to the organizer balance
        public async Task<bool> MarkTransactionCompletedAsync(string paymentReference)
        {
            var tx = await _context.CreditTransactions
                .FirstOrDefaultAsync(t => t.PaymentReference == paymentReference);

            if (tx == null || tx.Status == PaymentStatus.Paid)
                return false;

            tx.Status = PaymentStatus.Paid;
            tx.CompletedAt = DateTime.UtcNow;

            var balance = await _context.OrganizerCreditBalance
                .FirstOrDefaultAsync(b => b.OrganizerProfileId == tx.OrganizerProfileId);

            if (balance == null)
            {
                balance = new OrganizerCreditBalance
                {
                    Id = Guid.NewGuid(),
                    OrganizerProfileId = tx.OrganizerProfileId,
                    UserId = tx.UserId,
                    Balance = tx.CreditsChanged,
                    LastUpdated = DateTime.UtcNow
                };
                _context.OrganizerCreditBalance.Add(balance);
            }
            else
            {
                balance.Balance += tx.CreditsChanged;
                balance.LastUpdated = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            //for non blocking task  background notification with scope services
            _backgroundTaskQueue.QueueBackgroundWorkItem(
        async (sp, ct) =>
        {
            try
            {
                var bookingService = sp.GetRequiredService<IBookingService>();
                var notificationService = sp.GetRequiredService<INotificationService>();

                

                // Send notification
                await notificationService.SendNotificationAsync(
                    tx.UserId,
                    "Payment Successful",
                    $"{tx.CreditsChanged}-Credit Bought Successfully.",
                    NotificationType.General);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Background email/notification failed: {ex.Message}");
            }
        }
    );

            return true;
        }

        
        // GET BALANCE
    
        public async Task<int> GetBalanceAsync(Guid organizerId)
        {
            var balance = await _context.OrganizerCreditBalance
                .FirstOrDefaultAsync(b => b.UserId == organizerId);

            return balance?.Balance ?? 0;
        }

       //admin add credit manual using organizer user id
        public async Task AdminAddCreditsAsync(
   
   AdminAddCreditDto request)
        {
            if (request.Credits <= 0)
                throw new InvalidOperationException("Credits must be greater than zero.");

            //  Get organizer profile using UserId
            var organizerProfile = await _context.OrganizerProfiles
                .FirstOrDefaultAsync(o => o.UserId == request.UserId);

            if (organizerProfile == null)
                throw new InvalidOperationException("Organizer not found.");

            //  Get or create balance
            var balance = await _context.OrganizerCreditBalance
                .FirstOrDefaultAsync(b => b.OrganizerProfileId == organizerProfile.Id);

            if (balance == null)
            {
                balance = new OrganizerCreditBalance
                {
                    Id = Guid.NewGuid(),
                    OrganizerProfileId = organizerProfile.Id,
                    UserId = request.UserId,
                    Balance = request.Credits,
                    LastUpdated = DateTime.UtcNow
                };
                _context.OrganizerCreditBalance.Add(balance);
            }
            else
            {
                balance.Balance += request.Credits;
                balance.LastUpdated = DateTime.UtcNow;
            }

            //  Create transaction record (IMPORTANT)
            _context.CreditTransactions.Add(new CreditTransaction
            {
                Id = Guid.NewGuid(),
                OrganizerProfileId = organizerProfile.Id,
                UserId = request.UserId,
                CreditsChanged = request.Credits,
                Type = "AdminAdd",
                Description = request.Reason ?? "Admin added credits",
                Status = PaymentStatus.Paid,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }


        // DEDUCT CREDITS (for publish event, boost event, etc)
      
        public async Task DeductCreditsAsync(Guid userId, int credits, string type, string? description = null)
        {
            var balance = await _context.OrganizerCreditBalance
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (balance == null || balance.Balance < credits)
                throw new InvalidOperationException("Not enough credits");

            balance.Balance -= credits;
            balance.LastUpdated = DateTime.UtcNow;

            _context.CreditTransactions.Add(new CreditTransaction
            {
                Id = Guid.NewGuid(),
                OrganizerProfileId = balance.OrganizerProfileId, // FK must stay correct
                UserId = userId,
                CreditsChanged = -credits,
                Type = type,
                Description = description,
                Status = PaymentStatus.Paid,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }


        // GET TRANSACTION HISTORY
        
        public async Task<IEnumerable<CreditTransaction>> GetTransactionHistoryAsync(Guid organizerProfileId)
        {

            return await _context.CreditTransactions
                .Where(t => t.UserId == organizerProfileId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        
        // PLATFORM SETTINGS
        
        public async Task<PlatformSettings> GetPlatformSettingsAsync()
        {
            var settings = await _context.PlatformSettings.FirstOrDefaultAsync();
            if (settings == null)
                throw new InvalidOperationException("Platform settings not configured.");
            return settings;
        }

        // ADMIN REVENUE ANALYTICS

        public async Task<PlatformRevenueSummaryDto> GetPlatformRevenueSummaryAsync()
        {
            var settings = await GetPlatformSettingsAsync();
            var creditPrice = settings.CreditPriceETB;

            var result = new PlatformRevenueSummaryDto();

            // ---------------------------
            // Credits Purchased
            // ---------------------------
            var creditQuery = _context.CreditTransactions
                .AsNoTracking()
                .Where(t => t.Type == "Purchase" && t.Status == PaymentStatus.Paid);

            result.TotalCreditsPurchased = await creditQuery.SumAsync(t => t.CreditsChanged);
            result.TotalCreditRevenueETB = result.TotalCreditsPurchased * creditPrice;

            // ---------------------------
            // Events Published
            // ---------------------------
            result.TotalEventsPublished = await _context.Events.CountAsync();
            result.TotalPublishRevenueETB =
                result.TotalEventsPublished * (settings.EventPublishCost * creditPrice);

            // ---------------------------
            // Events Boosted
            // ---------------------------
            var boostQuery = _context.EventBoosts.AsNoTracking();

            var totalCreditsUsedForBoosts = await boostQuery.SumAsync(b => b.CreditsUsed);
            result.TotalBoostRevenueETB = totalCreditsUsedForBoosts * creditPrice;

            result.TotalEventsBoosted = await boostQuery
                .Select(b => b.EventId)
                .Distinct()
                .CountAsync();

            // ---------------------------
            // Weekly Revenue (Last 7 Days)
            // ---------------------------
            var sevenDaysAgo = DateTime.UtcNow.Date.AddDays(-6);

            result.WeeklyCreditRevenue = await creditQuery
                .Where(t => t.CreatedAt >= sevenDaysAgo)
                .GroupBy(t => t.CreatedAt.Date)
                .Select(g => new RevenueByWeekDto
                {
                    Date = g.Key,
                    RevenueETB = g.Sum(x => x.CreditsChanged * creditPrice)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            result.WeeklyBoostRevenue = await boostQuery
                .Where(b => b.StartTime >= sevenDaysAgo)
                .GroupBy(b => b.StartTime.Date)
                .Select(g => new RevenueByWeekDto
                {
                    Date = g.Key,
                    RevenueETB = g.Sum(x => x.CreditsUsed * creditPrice)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // ---------------------------
            // Monthly Revenue
            // ---------------------------
            result.MonthlyCreditRevenue = await creditQuery
                .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
                .Select(g => new RevenueByMonthDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    RevenueETB = g.Sum(x => x.CreditsChanged * creditPrice)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            result.MonthlyBoostRevenue = await boostQuery
                .GroupBy(b => new { b.StartTime.Year, b.StartTime.Month })
                .Select(g => new RevenueByMonthDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    RevenueETB = g.Sum(x => x.CreditsUsed * creditPrice)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            return result;
        }


        public async Task<BuyCreditInfoDto> GetPurchaseInfoAsync()
        {
            var settings = await GetPlatformSettingsAsync();

            return new BuyCreditInfoDto
            {
                CreditPriceETB = settings.CreditPriceETB,
                EventPublishCost = settings.EventPublishCost,
               
                Message = $"1 credit = {settings.CreditPriceETB} ETB"
            };
        }


        public async Task<List<CreditTransactionViewDto>> GetCreditTransactionsAsync(
      CreditTransactionFilterDto filter)
        {
            var query = _context.CreditTransactions
                .AsNoTracking()
                .Include(t => t.OrganizerProfile)
                .AsQueryable();

            // ---------------------------
            // Apply filters
            // ---------------------------
            if (!string.IsNullOrEmpty(filter.Type))
                query = query.Where(t => t.Type == filter.Type);

            if (filter.Status.HasValue)
                query = query.Where(t => t.Status == filter.Status.Value);

         

            if (filter.OrganizerUserId.HasValue)
                query = query.Where(t => t.UserId == filter.OrganizerUserId.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(t => t.CreatedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(t => t.CreatedAt <= filter.ToDate.Value);

            // ---------------------------
            // Projection (DTO)
            // ---------------------------
            return await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new CreditTransactionViewDto
                {
                    UserId = t.UserId,
                    Type = t.Type,
                    CreditsChanged = t.CreditsChanged,
                    TotalAmount = t.TotalAmount,
                    RefernceNumber=t.PaymentReference,
                    CheckoutUrl=t.ChapaCheckoutUrl,
                    Status = t.Status.ToString(),
                    OrganizerName = t.OrganizerProfile.BusinessName,
                    
                    CreatedAt = t.CreatedAt,
                    CompletedAt = t.CompletedAt
                })
                .ToListAsync();
        }



    }
}
