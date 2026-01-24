using Convene.Domain.Enums;
using System;
using System.Text.Json.Serialization;

namespace Convene.Application.DTOs.Event
{
    public class PricingRuleResponseDto
    {
        public Guid Id { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PricingRuleType RuleType { get; set; }
        public string? Description { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? PriceIncreasePercent { get; set; }
        public int? ThresholdPercentage { get; set; }
        public int? LastNDaysBeforeEvent { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}
