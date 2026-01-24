using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.Boosts;
using Convene.Application.Interfaces;

namespace Convene.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventBoostController : ControllerBase
    {
        private readonly IBoostService _boostService;

        public EventBoostController(IBoostService boostService)
        {
            _boostService = boostService;
        }

        // GET: api/EventBoost/Levels
        [HttpGet("Levels")]
        public async Task<IActionResult> GetBoostLevels()
        {
            var levels = await _boostService.GetActiveBoostLevelsAsync();
            return Ok(levels);
        }

        // POST: api/EventBoost/Apply
        [HttpPost("Apply")]
        [Authorize]
        public async Task<IActionResult> ApplyBoost([FromBody] ApplyBoostDto dto)
        {
            var organizerId = GetOrganizerProfileId();

            try
            {
                var eventBoost = await _boostService.ApplyBoostAsync(organizerId, dto.EventId, dto.BoostLevelId);
                return Ok(new
                {
                    Success = true,
                    Message = $"Event boosted successfully with {eventBoost.CreditsUsed} credits.",
                    EventBoostId = eventBoost.Id,
                    eventBoost.StartTime,
                    eventBoost.EndTime
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("MyBoosts")]
        [Authorize]
        public async Task<IActionResult> GetMyBoosts()
        {
            var organizerId = GetOrganizerProfileId();
            var boosts = await _boostService.GetOrganizerBoostsAsync(organizerId);

            var result = boosts.Select(b => new
            {
                BoostId = b.Id,
                b.EventId,
                EventTitle = b.Event.Title,
                BoostLevel = b.BoostLevel.Name,
                b.CreditsUsed,
                b.StartTime,
                b.EndTime,
                IsActive = DateTime.UtcNow < b.EndTime
            });

            return Ok(result);
        }

        private Guid GetOrganizerProfileId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(claim == null)
            {
                throw new Exception("Unauthorized Login and Try Again.");
            }   
            return Guid.Parse(claim!);
        }
    }
}
