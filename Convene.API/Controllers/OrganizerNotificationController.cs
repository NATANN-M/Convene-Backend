using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.OrganizerNotifications;
using Convene.Application.Interfaces;
using System.Security.Claims;

namespace Convene.API.Controllers.Organizer
{
    [ApiController]
    [Route("api/organizer/notifications")]
    [Authorize(Roles = "Organizer")]
    public class OrganizerNotificationController : ControllerBase
    {
        private readonly IOrganizerNotificationService _service;

        public OrganizerNotificationController(IOrganizerNotificationService service)
        {
            _service = service;
        }

        private Guid OrganizerId =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // 1?? Send notification
        [HttpPost("event")]
        public async Task<IActionResult> NotifyEventAttendees(
            OrganizerEventNotificationDto dto)
        {
            await _service.SendToEventAttendeesAsync(
                OrganizerId,
                dto.EventId,
                dto.Title,
                dto.Message);

            return Ok(new { message = "Notification sent to event attendees" });
        }

        // 2?? Organizer events (dropdown)
        [HttpGet("events")]
        public async Task<IActionResult> GetMyEvents()
        {
            var events = await _service.GetOrganizerEventsAsync(OrganizerId);
            return Ok(events);
        }
    }
}
