using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.Auth;
using Convene.Application.DTOs.Requests;
using Convene.Application.DTOs.Responses;
using Convene.Application.Interfaces;
using System.Threading.Tasks;

namespace Convene.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        
        [HttpPost("register-Attendee")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterUserRequest request)
        {
            var response = await _authService.RegisterAsync(request);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginUserRequest request)
        {
            var response = await _authService.LoginAsync(request);
            if (!response.Success)
                return Unauthorized(response);
            return Ok(response);
        }


        [HttpPost("register-organizer")]
        public async Task<IActionResult> RegisterOrganizer([FromForm] RegisterOrganizerRequest request)
        {
            var response = await _authService.RegisterOrganizerAsync(request);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequestDto dto)
        {
            await _authService.SendOtpAsync(dto);
            return Ok(new { Message = "OTP sent successfully." });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto dto)
        {
            await _authService.VerifyOtpAsync(dto);
            return Ok(new { Message = "OTP verified successfully." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            await _authService.ResetPasswordAsync(dto);
            return Ok(new { Message = "Password reset successful." });
        }

    }
}
