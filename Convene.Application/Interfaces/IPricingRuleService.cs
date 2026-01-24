using Convene.Application.DTOs.Event;
using System;
using System.Threading.Tasks;

namespace Convene.Application.Interfaces
{
    public interface IPricingRuleService
    {
        Task<PricingRuleResponseDto> AddPricingRuleAsync(Guid ticketTypeId, PricingRuleCreateDto dto);
        Task<PricingRuleResponseDto> UpdatePricingRuleAsync(Guid ruleId, PricingRuleCreateDto dto);
        Task<string> SetActiveStatusAsync(Guid ruleId, bool isActive);
        Task RemovePricingRuleAsync(Guid ruleId);

    }
}
