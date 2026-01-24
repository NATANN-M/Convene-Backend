using Convene.Application.DTOs.Event;
using System;
using System.Threading.Tasks;

namespace Convene.Application.Interfaces
{
    public interface IPricingService
    {
        Task<decimal> GetCurrentPriceAsync(Guid ticketTypeId);
        static void ValidatePricingRule(PricingRuleCreateDto dto) { }
    }
}
