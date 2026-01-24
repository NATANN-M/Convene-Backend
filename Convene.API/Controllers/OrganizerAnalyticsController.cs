using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.OrganizerAnalytics;
using Convene.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Convene.API.Controllers
{
    [ApiController]
    [Route("api/organizer/analytics")]
    [Authorize(Roles = "Organizer,SuperAdmin")]
    public class OrganizerAnalyticsController : ControllerBase
    {
        private readonly IOrganizerAnalyticsService _analyticsService;

        public OrganizerAnalyticsController(IOrganizerAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        
        [HttpGet("overview")]
        public async Task<ActionResult<OrganizerOverviewAnalyticsDto>> GetOverview()
        {
            var userClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userClaim)) return Unauthorized(new { message = "Unautherized Please Login And Try Again." });

            var organizerId = Guid.Parse(userClaim);
            var dto = await _analyticsService.GetOverviewAnalyticsAsync(organizerId);
            return Ok(dto);
        }


        [HttpGet("events")]
        public async Task<ActionResult<List<EventAnalyticsListItemDto>>> GetEvents()
        {
            var userClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userClaim)) return Unauthorized(new { message = "Unautherized Please Login And Try Again." });

            var organizerId = Guid.Parse(userClaim);
            var list = await _analyticsService.GetOrganizerAnalyticsEventsAsync(organizerId);
            return Ok(list);
        }

        
        [HttpGet("event/{eventId}")]
        public async Task<ActionResult<EventAnalyticsDto>> GetEventAnalytics(Guid eventId)
        {
            var userClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userClaim)) return Unauthorized(new { message = "Unautherized Please Login And Try Again." });

            var organizerId = Guid.Parse(userClaim);
            var dto = await _analyticsService.GetEventAnalyticsAsync(organizerId, eventId);
            return Ok(dto);
        }

        [HttpGet("event/{eventId}/booked-users")]
        public async Task<ActionResult<List<OrganizerBookedUserDto>>> GetBookedUsers(Guid eventId,DateTime ? startDate,DateTime ? endDate,Guid ? ticketTypeId)
        {
            var userClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userClaim == null)
                return Unauthorized(new { message = "Unautherized Please Login And Try Again." });

            Guid organizerId = Guid.Parse(userClaim);

            var users = await _analyticsService.GetBookedUsersAsync(organizerId, eventId,startDate,endDate,ticketTypeId);
            if (users == null )
            {
                return NotFound(new { message = "No booked users found for the specified Event or criteria." });
            }
            return Ok(users);
        }

        [HttpGet("{eventId}/booked-users/export")]
        public async Task<IActionResult> ExportBookedUsers(
    Guid eventId,
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate,
    [FromQuery] Guid? ticketTypeId,
    [FromQuery] string format = "csv")
        {
            var organizerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (organizerIdClaim == null)
                return Unauthorized();

            var organizerId = Guid.Parse(organizerIdClaim);

            var file = await _analyticsService.ExportBookedUsersAsync(
                organizerId,
                eventId,
                startDate,
                endDate,
                ticketTypeId,
                format
            );
            if (file == null || file.Bytes == null || file.Bytes.Length == 0)
            {
                return NotFound(new { message = "No data available for export with the specified criteria." });
            }

            return File(file.Bytes, file.ContentType, file.FileName);
        }

    }
}
