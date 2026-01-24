using Microsoft.EntityFrameworkCore;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Infrastructure.Persistence;
using Convene.Application.DTOs.AdminPlatformSetting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ML;
using Convene.Application.DTOs.Boosts.AdminBoostLevel;

namespace Convene.Infrastructure.Services
{
    public class BoostService : IBoostService
    {
        private readonly ConveneDbContext _context;
        private readonly ICreditService _creditService;

        public BoostService(ConveneDbContext context, ICreditService creditService)
        {
            _context = context;
            _creditService = creditService;
        }

        // Get active boost levels for organizers
        public async Task<IEnumerable<BoostLevel>> GetActiveBoostLevelsAsync()
        {
            return await _context.BoostLevels
                .Where(b => b.IsActive)
                .OrderBy(b => b.CreditCost)
                .ToListAsync();
        }

        // Check if organizer has enough credits
        public async Task<bool> CanApplyBoostAsync(Guid organizerProfileId, int boostCost)
        {
            var balance = await _creditService.GetBalanceAsync(organizerProfileId);
            return balance >= boostCost;
        }

        // Apply boost to an event
        public async Task<EventBoost> ApplyBoostAsync(Guid userId, Guid eventId, Guid boostLevelId)
        {
            var boostLevel = await _context.BoostLevels.FirstOrDefaultAsync(b => b.Id == boostLevelId && b.IsActive);
            if (boostLevel == null)
                throw new InvalidOperationException("Invalid or inactive boost level.");

            // Deduct credits
            await _creditService.DeductCreditsAsync(userId, boostLevel.CreditCost, "BoostEvent", $"Boost: {boostLevel.Name}");

            // Get OrganizerProfileId from UserId for FK references
            var organizerProfile = await _context.OrganizerProfiles
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (organizerProfile == null)
                throw new InvalidOperationException("Organizer profile not found for this user.");

            var now = DateTime.UtcNow;
            var eventBoost = new EventBoost
            {
                Id = Guid.NewGuid(),
                OrganizerProfileId = organizerProfile.Id,
                EventId = eventId,
                BoostLevelId = boostLevelId,
                CreditsUsed = boostLevel.CreditCost,
                StartTime = now,
                EndTime = now.AddHours(boostLevel.DurationHours)
            };

            _context.EventBoosts.Add(eventBoost);
            await _context.SaveChangesAsync();

            return eventBoost;
        }


        public async Task<IEnumerable<EventBoost>> GetOrganizerBoostsAsync(Guid userId)
        {
            // 1. Get the organizer profile from UserId
            var organizerProfile = await _context.OrganizerProfiles
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (organizerProfile == null)
                return new List<EventBoost>();

            var now = DateTime.UtcNow;

            // 2. Query using OrganizerProfileId
            return await _context.EventBoosts
                .Include(b => b.Event)
                .Include(b => b.BoostLevel)
                .Where(b => b.OrganizerProfileId == organizerProfile.Id)
                .OrderByDescending(b => b.StartTime)
                .ToListAsync();
        }



        // Admin analytics: summary of boosts

        public async Task<IEnumerable<BoostAnalyticsDto>> GetBoostAnalyticsAsync()
        {
            var creditPrice = (await _creditService.GetPlatformSettingsAsync()).CreditPriceETB;

            var analytics = await _context.EventBoosts
                .Where(b => b.BoostLevel != null)
                .GroupBy(b => new { b.BoostLevelId, b.BoostLevel.Name })
                .Select(g => new BoostAnalyticsDto
                {
                    BoostLevelId = g.Key.BoostLevelId,
                    BoostLevelName = g.Key.Name,
                    TotalTimesUsed = g.Count(),
                    TotalBoosts = g.Count(),
                    TotalCreditsUsed = g.Sum(x => x.CreditsUsed),
                    RevenueGeneratedETB = g.Sum(x => x.CreditsUsed) * creditPrice,
                    TotalEventsBoosted = g.Select(x => x.EventId).Distinct().Count()
                })
                .ToListAsync();

            // Optional: return empty list if no data
            return analytics ?? new List<BoostAnalyticsDto>();
        }






        // --- ADMIN CRUD IMPLEMENTATION ---
        public async Task<BoostLevelDto> CreateBoostLevelAsync(CreateBoostLevelDto dto)
    {
        var boost = new BoostLevel
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            CreditCost = dto.CreditCost,
            DurationHours = dto.DurationHours,
            Weight = dto.Weight,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.BoostLevels.Add(boost);
        await _context.SaveChangesAsync();

        return new BoostLevelDto
        {
            Id = boost.Id,
            Name = boost.Name,
            Description = boost.Description,
            CreditCost = boost.CreditCost,
            DurationHours = boost.DurationHours,
            Weight = boost.Weight,
            IsActive = boost.IsActive,
            CreatedAt = boost.CreatedAt
        };
    }

    public async Task<BoostLevelDto> UpdateBoostLevelAsync(UpdateBoostLevelDto dto)
    {
        var boost = await _context.BoostLevels.FirstOrDefaultAsync(b => b.Id == dto.Id);
        if (boost == null)
            throw new KeyNotFoundException("Boost level not found.");

        boost.Name = dto.Name;
        boost.Description = dto.Description;
        boost.CreditCost = dto.CreditCost;
        boost.DurationHours = dto.DurationHours;
        boost.Weight = dto.Weight;
        boost.IsActive = dto.IsActive;
        boost.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new BoostLevelDto
        {
            Id = boost.Id,
            Name = boost.Name,
            Description = boost.Description,
            CreditCost = boost.CreditCost,
            DurationHours = boost.DurationHours,
            Weight = boost.Weight,
            IsActive = boost.IsActive,
            CreatedAt = boost.CreatedAt,
            UpdatedAt = boost.UpdatedAt
        };
    }

    public async Task<bool> DeleteBoostLevelAsync(Guid boostLevelId)
    {
        var boost = await _context.BoostLevels.FirstOrDefaultAsync(b => b.Id == boostLevelId);
        if (boost == null)
            return false;

        // Soft delete
        boost.IsActive = false;
        boost.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<BoostLevelDto>> GetAllBoostLevelsAsync()
    {
        return await _context.BoostLevels
            .Select(b => new BoostLevelDto
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                CreditCost = b.CreditCost,
                DurationHours = b.DurationHours,
                Weight = b.Weight,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<BoostLevelDto> GetBoostLevelByIdAsync(Guid boostLevelId)
    {
        var b = await _context.BoostLevels.FirstOrDefaultAsync(b => b.Id == boostLevelId);
        if (b == null)
            throw new KeyNotFoundException("Boost level not found.");

        return new BoostLevelDto
        {
            Id = b.Id,
            Name = b.Name,
            Description = b.Description,
            CreditCost = b.CreditCost,
            DurationHours = b.DurationHours,
            Weight = b.Weight,
            IsActive = b.IsActive,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt
        };
    }

        public async Task<bool> SetBoostLevelStatusAsync(Guid boostLevelId, bool isActive)
        {
            var boost = await _context.BoostLevels
                .FirstOrDefaultAsync(b => b.Id == boostLevelId);

            if (boost == null)
                return false;

            // If already same state, no need to update
            if (boost.IsActive == isActive)
                return true;

            boost.IsActive = isActive;
            boost.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }


    }
}
