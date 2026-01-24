using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.Event;
using Convene.Application.Interfaces;

namespace Convene.API.Controllers
{
    [ApiController]
    [Route("api/events")]
    [Authorize(Roles = "Organizer,SuperAdmin")]
    public class PricingRuleController : ControllerBase
    {
        private readonly IPricingRuleService _pricingRuleService;

        public PricingRuleController(IPricingRuleService pricingRuleService)
        {
            _pricingRuleService = pricingRuleService;
        }

        [HttpPost("add-pricing-rule-to-ticket-type/{ticketTypeId}")]
        public async Task<IActionResult> AddPricingRuleToTicketType(Guid ticketTypeId, [FromBody] PricingRuleCreateDto dto)
        {
            var rule = await _pricingRuleService.AddPricingRuleAsync(ticketTypeId, dto);
            return Ok(rule);
        }

        [HttpPut("update-pricing-rule/{ruleId}")]
        public async Task<IActionResult> UpdatePricingRule(Guid ruleId, [FromBody] PricingRuleCreateDto dto)
        {
            var rule = await _pricingRuleService.UpdatePricingRuleAsync(ruleId, dto);
            return Ok(rule);
        }

        [HttpDelete("remove-pricing-rule/{ruleId}")]
        public async Task<IActionResult> RemovePricingRule(Guid ruleId)
        {
            await _pricingRuleService.RemovePricingRuleAsync(ruleId);
            return Ok(new { message = "Pricing rule removed successfully" });
        }

        [HttpPatch("set-status/{ruleId}")]
        public async Task<IActionResult> SetActiveStatus(Guid ruleId, [FromQuery] bool isActive)
        {
            var message = await _pricingRuleService.SetActiveStatusAsync(ruleId, isActive);
            return Ok(new { Message = message });
        }
    }

}

