using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Application.Interfaces;
using Convene.Domain.Enums;

namespace Convene.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUserNotifications()
        {
            var userIdclaim =User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdclaim))
            {
                return Unauthorized(new { Massage = "Unautherized Please Login And Try Again." });

            }

            var userId = Guid.Parse(userIdclaim);
            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            return Ok(notifications);
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return Ok(new { message = "Notification marked as read" });
        }

        [HttpPost("test")]
        public async Task<IActionResult> TestNotification()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("Unautherized Please Login And Try Again.");

            Guid userId = Guid.Parse(userIdClaim);
            await _notificationService.SendNotificationAsync(userId, "Test", "This is a test notification", NotificationType.General);
            return Ok("Notification sent");
        }

    }
}
