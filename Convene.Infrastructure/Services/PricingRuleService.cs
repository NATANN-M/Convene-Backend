using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.Event;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Infrastructure.Persistence;

namespace Convene.Infrastructure.Services
{
    public class PricingRuleService : IPricingRuleService
    {
        private readonly ConveneDbContext _context;

        public PricingRuleService(ConveneDbContext context)
        {
            _context = context;
        }

        public async Task<PricingRuleResponseDto> AddPricingRuleAsync(Guid ticketTypeId, PricingRuleCreateDto dto)
        {
            PricingService.ValidatePricingRule(dto);

            var rule = new DynamicPricingRule
            {
                Id = Guid.NewGuid(),
                TicketTypeId = ticketTypeId,
                RuleType = dto.RuleType,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                DiscountPercent = dto.DiscountPercent,
                LastNDaysBeforeEvent = dto.LastNDaysBeforeEvent,
                ThresholdPercentage = dto.ThresholdPercentage,
                PriceIncreasePercent = dto.PriceIncreasePercent,
                IsActive = true
            };

            await _context.DynamicPricingRules.AddAsync(rule);
            await _context.SaveChangesAsync();

            return new PricingRuleResponseDto
            {
                Id = rule.Id,
                RuleType = rule.RuleType,
                Description = rule.Description,
                StartDate = rule.StartDate,
                EndDate = rule.EndDate,
                DiscountPercent = rule.DiscountPercent,
                LastNDaysBeforeEvent = rule.LastNDaysBeforeEvent,
                ThresholdPercentage = rule.ThresholdPercentage,
                PriceIncreasePercent = rule.PriceIncreasePercent,
                IsActive = rule.IsActive
            };
        }

        public async Task<PricingRuleResponseDto> UpdatePricingRuleAsync(Guid ruleId, PricingRuleCreateDto dto)
        {
            var rule = await _context.DynamicPricingRules.FirstOrDefaultAsync(r => r.Id == ruleId);
            if (rule == null) throw new KeyNotFoundException("Pricing rule not found");

            PricingService.ValidatePricingRule(dto);

            rule.RuleType = dto.RuleType;
            rule.Description = dto.Description;
            rule.StartDate = dto.StartDate;
            rule.EndDate = dto.EndDate;
            rule.DiscountPercent = dto.DiscountPercent;
            rule.LastNDaysBeforeEvent = dto.LastNDaysBeforeEvent;
            rule.ThresholdPercentage = dto.ThresholdPercentage;
            rule.PriceIncreasePercent = dto.PriceIncreasePercent;

            await _context.SaveChangesAsync();

            return new PricingRuleResponseDto
            {
                Id = rule.Id,
                RuleType = rule.RuleType,
                Description = rule.Description,
                StartDate = rule.StartDate,
                EndDate = rule.EndDate,
                DiscountPercent = rule.DiscountPercent,
                LastNDaysBeforeEvent = rule.LastNDaysBeforeEvent,
                ThresholdPercentage = rule.ThresholdPercentage,
                PriceIncreasePercent = rule.PriceIncreasePercent,
                IsActive = rule.IsActive
            };
        }

        public async Task RemovePricingRuleAsync(Guid ruleId)
        {
            var rule = await _context.DynamicPricingRules.FirstOrDefaultAsync(r => r.Id == ruleId);
            if (rule == null) throw new KeyNotFoundException("Pricing rule not found");

            _context.DynamicPricingRules.Remove(rule);
            await _context.SaveChangesAsync();
        }

        public async Task<string> SetActiveStatusAsync(Guid ruleId, bool isActive)
        {
            var rule = await _context.DynamicPricingRules.FirstOrDefaultAsync(r => r.Id == ruleId);
            if (rule == null) throw new KeyNotFoundException("Pricing rule not found");

            rule.IsActive = isActive;
            await _context.SaveChangesAsync();

            return isActive ? "Pricing rule activated successfully." : "Pricing rule deactivated successfully.";
        
    }

    }

}
