using Convene.Domain.Common;
using Convene.Domain.Enums;
using System;

namespace Convene.Domain.Entities
{
    public class DynamicPricingRule : BaseEntity
    {
        public Guid TicketTypeId { get; set; }
        public PricingRuleType RuleType { get; set; } = PricingRuleType.None;
        public string? Description { get; set; }

        // Early Bird Rule
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? DiscountPercent { get; set; }

        // Last Minute Rule
        public int? LastNDaysBeforeEvent { get; set; }  // Example: 3 days before event

        // Demand Based Rule
        public int? ThresholdPercentage { get; set; }   // Example: 70 => after 70% sold
        public decimal? PriceIncreasePercent { get; set; } // Example: +15%

        public bool IsActive { get; set; } = true;

        // Navigation
        public TicketType? TicketType { get; set; }
    }
}
