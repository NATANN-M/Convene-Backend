using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.Event;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Domain.Enums;
using Convene.Infrastructure.Persistence;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Services
{
    public class PricingService : IPricingService
    {
        private readonly ConveneDbContext _context;

        public PricingService(ConveneDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Calculates the current ticket price based on all active dynamic pricing rules.
        /// </summary>
        public async Task<decimal> GetCurrentPriceAsync(Guid ticketTypeId)
        {
            var ticketType = await _context.TicketTypes
                .Include(t => t.Event)
                .Include(t => t.PricingRules)
                .FirstOrDefaultAsync(t => t.Id == ticketTypeId);

            if (ticketType == null)
                throw new Exception("Ticket type not found.");

            var now = DateTime.UtcNow;
            decimal basePrice = ticketType.BasePrice;
            decimal price = basePrice;

            // === EARLY BIRD DISCOUNT ===
            var earlyBird = ticketType.PricingRules
                .FirstOrDefault(r => r.IsActive &&
                                     r.RuleType == PricingRuleType.EarlyBird &&
                                     r.StartDate <= now && r.EndDate >= now);

            if (earlyBird?.DiscountPercent is > 0)
                price -= basePrice * (earlyBird.DiscountPercent.Value / 100m);

            // === LAST MINUTE DISCOUNT ===
            var lastMinute = ticketType.PricingRules
                .FirstOrDefault(r => r.IsActive && r.RuleType == PricingRuleType.LastMinute);

            if (lastMinute?.LastNDaysBeforeEvent is > 0 &&
                ticketType.Event != null &&
                lastMinute.DiscountPercent.HasValue)
            {
                var daysBefore = (ticketType.Event.StartDate - now).TotalDays;
                if (daysBefore <= lastMinute.LastNDaysBeforeEvent.Value && daysBefore >= 0)
                    price -= basePrice * (lastMinute.DiscountPercent.Value / 100m);
            }

            // === DEMAND BASED INCREASE ===
            var demandRule = ticketType.PricingRules
                .FirstOrDefault(r => r.IsActive && r.RuleType == PricingRuleType.DemandBased);

            if (demandRule?.ThresholdPercentage is > 0 &&
                demandRule.PriceIncreasePercent is > 0 &&
                ticketType.Quantity > 0)
            {
                var soldPercent = (ticketType.Sold / (decimal)ticketType.Quantity) * 100m;
                if (soldPercent >= demandRule.ThresholdPercentage.Value)
                    price += basePrice * (demandRule.PriceIncreasePercent.Value / 100m);
            }

            return Math.Round(price, 2);
        }

        /// <summary>
        /// Validates that a pricing rule DTO has all required fields for its type.
        /// </summary>
        public static void ValidatePricingRule(PricingRuleCreateDto dto)
        {
            switch (dto.RuleType)
            {
                case PricingRuleType.EarlyBird:
                    if (!dto.StartDate.HasValue || !dto.EndDate.HasValue)
                        throw new Exception("Early Bird rule requires StartDate and EndDate.");

                    if (!dto.DiscountPercent.HasValue || dto.DiscountPercent.Value <= 0)
                        throw new Exception("Early Bird rule requires a DiscountPercent greater than 0.");

                    if (dto.EndDate <= dto.StartDate)
                        throw new Exception("EndDate must be after StartDate.");
                    break;

                case PricingRuleType.LastMinute:
                    if (!dto.LastNDaysBeforeEvent.HasValue || dto.LastNDaysBeforeEvent.Value <= 0)
                        throw new Exception("Last Minute rule requires LastNDaysBeforeEvent greater than 0.");

                    if (!dto.DiscountPercent.HasValue || dto.DiscountPercent.Value <= 0)
                        throw new Exception("Last Minute rule requires a DiscountPercent greater than 0.");
                    break;

                case PricingRuleType.DemandBased:
                    if (!dto.ThresholdPercentage.HasValue || dto.ThresholdPercentage.Value <= 0)
                        throw new Exception("Demand Based rule requires ThresholdPercentage greater than 0.");

                    if (!dto.PriceIncreasePercent.HasValue || dto.PriceIncreasePercent.Value <= 0)
                        throw new Exception("Demand Based rule requires PriceIncreasePercent greater than 0.");
                    break;

                default:
                    throw new Exception("Invalid pricing rule type.");
            }
        }

    }
}
