using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;
using Convene.Application.DTOs.Recommendation;
using Convene.Application.Interfaces;

[ApiController]
[Route("api/track")]
public class TrackingController : ControllerBase
{
    private readonly ITrackingService _trackingService;

    public TrackingController(ITrackingService trackingService)
    {
        _trackingService = trackingService;
    }

    
  
    [HttpPost]
    public async Task<IActionResult> TrackInteraction([FromBody] TrackInteractionDto dto)
    {
        var userClaim=User.FindFirstValue(ClaimTypes.NameIdentifier);

        if(string.IsNullOrEmpty(userClaim))
            return Unauthorized(new { message = "Unauthorized Please Login And Try Again" });

        Guid userId = Guid.Parse(userClaim);


        if (dto == null)
            return BadRequest("Payload cannot be null.");

        await _trackingService.SaveInteractionAsync(dto ,userId);

        return Ok(new { message = "Interaction tracked successfully." });
    }

  
    [HttpPost("location")]
    public async Task<IActionResult> TrackUserLocation([FromBody] TrackUserLocationDto dto)
    {
        var userClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userClaim))
            Unauthorized(new { message = "Unauthorized Please Login And Try Again." });

        
        Guid userId = Guid.Parse(userClaim);

        if (string.IsNullOrEmpty(dto.UserLocation))
            return BadRequest("UserLocation is required.");

        await _trackingService.UpdateUserLocationOnlyAsync(userId, dto.UserLocation);

        return Ok(new { message = "User location updated successfully." });
    }
}
