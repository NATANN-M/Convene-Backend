using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.EventBrowsing;
using Convene.Application.DTOs.PagenationDtos;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;

namespace Convene.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class EventBrowsingController : ControllerBase
    {
        private readonly IEventBrowsingService _eventBrowsingService;

        public EventBrowsingController(IEventBrowsingService eventBrowsingService)
        {
            _eventBrowsingService = eventBrowsingService;
        }

        // Get all active events (not ended yet) with pagination
        [HttpGet("activeEvents")]
        public async Task<ActionResult<PaginatedResult<EventSummaryDto>>> GetActiveEvents([FromQuery] PagedAndSortedRequest request)
        {
            var events = await _eventBrowsingService.GetActiveEventsAsync(request);
            return Ok(events);
        }

        [HttpGet("detailEvent/{eventId}")]
        public async Task<ActionResult<EventDetailDto>> GetEventDetails(Guid eventId)
        {
            try
            {
                var userClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Guid? userid = null;

                if (!string.IsNullOrEmpty(userClaim) && Guid.TryParse(userClaim, out Guid parsedUserId))
                {
                    userid = parsedUserId;
                }

                var eventDetail = await _eventBrowsingService.GetEventDetailsAsync(eventId, userid);
                return Ok(eventDetail);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "Event not found." });
            }
        }

        [HttpGet("upcomingEvents")]
        public async Task<ActionResult<PaginatedResult<EventSummaryDto>>> GetUpcomingEvents([FromQuery] PagedAndSortedRequest request)
        {
            var events = await _eventBrowsingService.GetUpcomingEventsAsync(request);
            return Ok(events);
        }

        [HttpPost("Eventsearch")]
        public async Task<ActionResult<PaginatedResult<EventSummaryDto>>> SearchEvents([FromBody] EventSearchRequestDto req)
        {
            var result = await _eventBrowsingService.SearchEventsAsync(req);
            return Ok(result);
        }
    }
}
