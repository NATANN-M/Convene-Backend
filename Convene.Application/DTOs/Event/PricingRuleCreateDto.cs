using Convene.Domain.Enums;
using System;
using System.Text.Json.Serialization;

namespace Convene.Application.DTOs.Event
{
    public class PricingRuleCreateDto
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PricingRuleType RuleType { get; set; } = PricingRuleType.None;
        public string? Description { get; set; }

        // Early Bird
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? DiscountPercent { get; set; }

        // Last Minute
        public int? LastNDaysBeforeEvent { get; set; }

        // Demand Based
        public int? ThresholdPercentage { get; set; }
        public decimal? PriceIncreasePercent { get; set; }
    }
}
