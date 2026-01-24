using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.Organizer;
using Convene.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/organizer/dashboard")]
[Authorize(Roles = "Organizer")]
public class OrganizerDashboardController : ControllerBase
{
    private readonly IOrganizerDashboardService _dashboardService;

    public OrganizerDashboardController(IOrganizerDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("my-events")]
  
    public async Task<ActionResult<List<OrganizerDashboardEventDto>>> GetDashboardEvents()
    {
        
        var userclaim=User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userclaim == null)
        {
            return Unauthorized( new {message= "Unautherized Please Login And Try Again." });
        }

        Guid organizerId = Guid.Parse(userclaim);

        var events = await _dashboardService.GetDashboardEventsAsync(organizerId);
        return Ok(events);
    }
}
