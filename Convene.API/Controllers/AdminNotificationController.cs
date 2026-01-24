using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.AdminNotifications;
using Convene.Application.Interfaces;

namespace Convene.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/notifications")]
   // [Authorize(Roles = "SuperAdmin,Admin")]
    public class AdminNotificationController : ControllerBase
    {
        private readonly IAdminNotificationService _service;

        public AdminNotificationController(IAdminNotificationService service)
        {
            _service = service;
        }

        // 1?? Broadcast
        [HttpPost("broadcast")]
        public async Task<IActionResult> SendBroadcast(
            AdminBroadcastNotificationDto dto)
        {
            await _service.SendBroadcastAsync(dto.Title, dto.Message);
            return Ok(new { message = "Notification sent to all users" });
        }

        // 2?? Direct to users
        [HttpPost("users")]
        public async Task<IActionResult> SendToUsers(
            AdminDirectNotificationDto dto)
        {
            await _service.SendToUsersAsync(
                dto.UserIds,
                dto.Title,
                dto.Message);

            return Ok(new { message = "Notification sent to selected users" });
        }

        // 3?? User list + search
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? search,
            [FromQuery] string? category)
        {
            var users = await _service.GetUsersAsync(search, category);
            return Ok(users);
        }
    }
}
