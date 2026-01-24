using Microsoft.EntityFrameworkCore;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Infrastructure.Persistence;
using Convene.Application.DTOs.AdminPlatformSetting;
using System;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Services
{
    public class PlatformSettingsService : IPlatformSettingsService
    {
        private readonly ConveneDbContext _context;

        public PlatformSettingsService(ConveneDbContext context)
        {
            _context = context;
        }

        // Get full platform settings
        public async Task<PlatformSettings> GetSettingsAsync()
        {
            return await _context.PlatformSettings.FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("Platform settings not found.");
        }

        // Update platform settings from DTO
        public async Task UpdateSettingsAsync(UpdatePlatformSettingsDto dto)
        {
            var settings = await _context.PlatformSettings.FirstOrDefaultAsync();
            if (settings == null)
                throw new InvalidOperationException("Platform settings not found.");

            settings.CreditPriceETB = dto.CreditPriceETB;
            settings.InitialOrganizerCredits = dto.InitialOrganizerCredits;
            settings.EventPublishCost = dto.EventPublishCost;
            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        // Convenience: get cost to publish event
        public async Task<int> GetEventPublishCostAsync()
        {
            var settings = await GetSettingsAsync();
            return settings.EventPublishCost;
        }

        // Convenience: get initial credits for a new organizer
        public async Task<int> GetInitialOrganizerCreditsAsync()
        {
            var settings = await GetSettingsAsync();
            return settings.InitialOrganizerCredits;
        }

        // Convenience: get credit price in ETB
        public async Task<decimal> GetCreditPriceETBAsync()
        {
            var settings = await GetSettingsAsync();
            return settings.CreditPriceETB;
        }
    }
}
