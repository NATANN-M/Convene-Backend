using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.Feedback;
using Convene.Application.Interfaces;
using Convene.Infrastructure.Persistence;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Convene.API.Controllers
{
    [ApiController]
    [Route("api/feedback")]
    public class EventFeedbackController : ControllerBase
    {
        private readonly IEventFeedbackService _feedbackService;

        public EventFeedbackController(IEventFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        private Guid GetUserId()
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(id))
                throw new UnauthorizedAccessException("Unauthorized Login and Try Again.");
            return Guid.Parse(id!);
        }

        [Authorize]
        [HttpGet("eligible-for-feedback")]
        public async Task<IActionResult> GetEligibleEvents()
        {
            var userId = GetUserId();
            var events = await _feedbackService.GetEligibleEventsForFeedbackAsync(userId);
            return Ok(events);
        }

       
        [Authorize]
        [HttpPost("{eventId}")]
        public async Task<IActionResult> Submit(Guid eventId, [FromBody] CreateFeedbackDto dto)
        {
            var userId = GetUserId();
            var result = await _feedbackService.SubmitFeedbackAsync(eventId, dto, userId);
            return Ok(result);
        }

        [Authorize]
        [HttpPut("{feedbackId}")]
        public async Task<IActionResult> Update(Guid feedbackId, [FromBody] UpdateFeedbackDto dto)
        {
            var userId = GetUserId();
            var result = await _feedbackService.UpdateFeedbackAsync(feedbackId, dto, userId);
            return Ok(result);
        }

        
        [Authorize]
        [HttpDelete("{feedbackId}")]
        public async Task<IActionResult> Delete(Guid feedbackId)
        {
            var userId = GetUserId();
            var ok = await _feedbackService.DeleteFeedbackAsync(feedbackId, userId);
            if (!ok) return NotFound();
            return Ok(new { message = "Feedback deleted" });
        }

        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetEventFeedbacks(Guid eventId)
        {
            var list = await _feedbackService.GetFeedbacksForEventAsync(eventId);
            return Ok(list);
        }

        [HttpGet("organizersAll-Events-FeedBacks/{organizerId}")]
        public async Task<IActionResult> GetOrganizerFeedbacks(Guid organizerId)
        {
            var result = await _feedbackService.GetFeedbacksForOrganizerAsync(organizerId);
            return Ok(result);
        }


       
        [HttpGet("event/{eventId}/summary")]
        public async Task<IActionResult> GetSummary(Guid eventId)
        {
            var summary = await _feedbackService.GetEventFeedbackSummaryAsync(eventId);
            return Ok(summary);
        }

       
        [Authorize]
        [HttpGet("getMyfeedback/{eventId}")]
        public async Task<IActionResult> MyFeedbackForEvent(Guid eventId)
        {
            var userId = GetUserId();
            var result = await _feedbackService.GetUserFeedbackForEventAsync(userId, eventId);
            return Ok(result);
        }

      
        [Authorize]
        [HttpGet("myFeedbacks/history")]
        public async Task<IActionResult> MyHistory()
        {
            var userId = GetUserId();
            var list = await _feedback_service_GetUserHistory(userId);
            return Ok(list);
        }

        // small wrapper to decouple 
        private Task<List<FeedbackViewDto>> _feedback_service_GetUserHistory(Guid userId) =>
            _feedbackService.GetUserFeedbackHistoryAsync(userId);
    }



}
