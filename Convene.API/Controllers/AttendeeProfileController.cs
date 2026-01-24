using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.AttendeeProfile;
using Convene.Application.Interfaces;

namespace Convene.API.Controllers
{
    [ApiController]
    [Route("api/attendee/profile")]
    [Authorize(Roles = "Attendee")]
    public class AttendeeProfileController : ControllerBase
    {
        private readonly IAttendeeProfileService _service;

        public AttendeeProfileController(IAttendeeProfileService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userClaims = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (String.IsNullOrEmpty(userClaims)) return Unauthorized(new { message = "Unauthorized Login and Try again." });
            var userId = Guid.Parse(userClaims);
            var result = await _service.GetProfileAsync(userId);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateAttendeeProfileDto dto)
        {
            var userClaims = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (String.IsNullOrEmpty(userClaims)) return Unauthorized(new { message = "Unauthorized Login and Try again." });
            var userId = Guid.Parse(userClaims);
            await _service.UpdateProfileAsync(userId, dto);
            return Ok("Profile updated successfully");
        }

        [HttpPut("image")]
        public async Task<IActionResult> UpdateImage(IFormFile file)
        {
            var userClaims = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (String.IsNullOrEmpty(userClaims)) return Unauthorized(new { message = "Unauthorized Login and Try again." });
            var userId = Guid.Parse(userClaims);
            var url = await _service.UpdateProfileImageAsync(userId, file);
            return Ok(new AttendeeProfileImageResponseDto { ProfileImageUrl = url });
        }
    }

}
