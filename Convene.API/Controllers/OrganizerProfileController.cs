using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.OrganizerProfile;
using Convene.Application.Interfaces;

namespace Convene.API.Controllers
{
    [ApiController]
    [Route("api/organizer/profile")]
    [Authorize(Roles = "Organizer,SuperAdmin")]
    public class OrganizerProfileController : ControllerBase
    {
        private readonly IOrganizerProfileService _service;

        public OrganizerProfileController(IOrganizerProfileService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userClaims=User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(String.IsNullOrEmpty(userClaims)) return Unauthorized(new {message= "Unautherized Please Login And Try Again." });
            var userId = Guid.Parse(userClaims);
            var result = await _service.GetProfileAsync(userId);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateOrganizerProfileDto dto)
        {
            var userClaims = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (String.IsNullOrEmpty(userClaims)) return Unauthorized(new { message = "Unautherized Please Login And Try Again." });
            var userId = Guid.Parse(userClaims);
            await _service.UpdateProfileAsync(userId, dto);
            return Ok("Profile updated successfully");
        }

        [HttpPut("image")]
        public async Task<IActionResult> UpdateImage(IFormFile file)
        {
            var userClaims = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (String.IsNullOrEmpty(userClaims)) return Unauthorized(new { message = "Unautherized Please Login And Try Again." });
            var userId = Guid.Parse(userClaims);
            var url = await _service.UpdateProfileImageAsync(userId, file);
            return Ok(new OrganizerProfileImageResponseDto { ProfileImageUrl = url });
        }
    }

}
