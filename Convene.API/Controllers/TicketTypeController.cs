using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.Event;
using Convene.Application.Interfaces;

[ApiController]
[Route("api/events")]
[Authorize(Roles = "Organizer,SuperAdmin")]
public class TicketTypeController : ControllerBase
{
    private readonly ITicketTypeService _ticketTypeService;

    public TicketTypeController(ITicketTypeService ticketTypeService)
    {
        _ticketTypeService = ticketTypeService;
    }

    [HttpPost("add-ticket-type-to-event/{eventId}")]
    public async Task<IActionResult> AddTicketTypeToEvent(Guid eventId, [FromBody] TicketTypeCreateDto dto)
    {
        var ticket = await _ticketTypeService.AddTicketTypeAsync(eventId, dto);
        return Ok(ticket);
    }

    [HttpPut("update-ticket-type/{ticketTypeId}")]
    public async Task<IActionResult> UpdateTicketType(Guid ticketTypeId, [FromBody] TicketTypeCreateDto dto)
    {
        var ticket = await _ticketTypeService.UpdateTicketTypeAsync(ticketTypeId, dto);
        return Ok(ticket);
        
    }

    // Activate or Deactivate
    [HttpPatch("set-active-status-ticket/{ticketTypeId}")]
    public async Task<IActionResult> SetActiveStatus(Guid ticketTypeId, [FromQuery] bool isActive)
    {
        var message = await _ticketTypeService.SetActiveStatusAsync(ticketTypeId, isActive);
        return Ok(new { Message = message });
    }

    // Remove
    [HttpDelete("remove-ticket-type/{ticketTypeId}")]
    public async Task<IActionResult> RemoveTicketType(Guid ticketTypeId)
    {
        var message = await _ticketTypeService.RemoveTicketTypeAsync(ticketTypeId);
        return Ok(new { Message = message });
    }

}
