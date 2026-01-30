using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.Event;
using Convene.Application.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Convene.API.Controllers
{
    [ApiController]
    [Route("api/events")]
  [Authorize(Roles = "Organizer,SuperAdmin,Admin")]
    public class EventController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly ITelegramService _telegramService;

        public EventController(IEventService eventService,ITelegramService telegramService)
        {
            _eventService = eventService;
            _telegramService = telegramService;
        }

        [HttpPost("create-event")]
        public async Task<IActionResult> CreateEvent([FromForm] EventCreateDto dto)
        {
            var organizerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(organizerIdClaim))
                return Unauthorized(new { message = "Unauthorized Please Login And Try Again." });



            var organizerId = Guid.Parse(organizerIdClaim);
            var createdEvent = await _eventService.CreateEventAsync(dto, organizerId);
            return Ok(createdEvent);
        }



        [HttpPut("update-event/{eventId}")]
        public async Task<IActionResult> UpdateEvent(Guid eventId, [FromForm] EventUpdateDto dto)
        {
           

            var updatedEvent = await _eventService.UpdateEventAsync(dto,eventId);
            return Ok(updatedEvent);
        }

        [HttpPost("publish-event/{eventId}")]
        public async Task<IActionResult> PublishEvent(Guid eventId)
        {
            var success = await _eventService.PublishEventAsync(eventId);
            return Ok(new { Success = success, Message = "Event published successfully." });
        }

        [HttpGet("get-event-by-id/{eventId}")]
        public async Task<IActionResult> GetEventById(Guid eventId)
        {
            var ev = await _eventService.GetEventByIdAsync(eventId);
            return Ok(ev);
        }

        [HttpGet("get-events-by-organizer/{organizerId}")]
        public async Task<IActionResult> GetEventsByOrganizer(Guid organizerId)
        {
            

            var events = await _eventService.GetOrganizerEventsAsync(organizerId);
            return Ok(events);
        }
        [AllowAnonymous]
        [HttpGet("get-default-ticket-types")]
        public IActionResult GetDefaultTicketTypes()
        {
            var defaults = _eventService.GetDefaultTicketTypes();
            return Ok(defaults);
        }

        [HttpDelete("delete-draft-event")]
        public async Task<IActionResult> DeleteDraftEvent(Guid eventId)
        {
            var organizerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(organizerIdClaim))
                return Unauthorized(new { message = "Unauthorized Please Login And Try Again." });

            var organizerId = Guid.Parse(organizerIdClaim);

            var deletedraft = await _eventService.DeleteDraftEventAsync(eventId, organizerId);

            if (deletedraft)
                return Ok("Draft Event Successfully Deleted");

            return BadRequest("Failed to delete draft event");
        }

        [AllowAnonymous]
        [HttpGet("get-default-pricing-rules")]
        public IActionResult GetDefaultPricingRules()
        {
            var defaults = _eventService.GetDefaultPricingRules();
            return Ok(defaults);
        }


        //[HttpPost("post-event-Telgram/{eventId}")]
        //public async Task<IActionResult> PublishEventToTelegram(Guid eventId)
        //{
        //    var dto = await _eventService.CompileEventTelegramDataAsync(eventId);
        //    if (dto == null)
        //        return NotFound("Event not found");

        //    await _telegramService.SendEventToChannelAsync(dto);
        //    return Ok("Event posted to Telegram channel successfully");
        //}


        [HttpPost("post-event-Telegram/{eventId}")]
        public async Task<IActionResult> PublishEventToTelegram(Guid eventId)
        {
            try
            {
                var dto = await _eventService.PublishEventToTelegramAsync(eventId);
                if (dto == null)
                    return NotFound("Event not found");

                return Ok("Event posted to Telegram channel successfully");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }


    }
}
