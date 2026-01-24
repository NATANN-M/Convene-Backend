using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.GatePerson;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/gatepersons")]
[Authorize(Roles ="Organizer")]
public class GatePersonController : ControllerBase
{
    private readonly IGatePersonService _service;

    public GatePersonController(IGatePersonService service)
    {
        _service = service;
    }


   
    [HttpGet("get-assignment-events")]
    public async Task<IActionResult> GetAssignmentEvents()
    {
        var organizerIdClaims = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if(string.IsNullOrEmpty(organizerIdClaims))
        {
            return Unauthorized(new { message = " Unauthorized Please Login And Try Again." });
        }
        var organizerId=Guid.Parse(organizerIdClaims);

        var events = await _service.GetEventsForAssignmentAsync(organizerId);
    

        return Ok(events);
    }


    [HttpPost("createScanner")]
    public async Task<IActionResult> Create([FromBody] GatePersonCreateUpdateDto dto)
    {
        var organizerClaims = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(organizerClaims))
        {
            return Unauthorized(new { message = " Unauthorized Please Login And Try Again." });
        }

        var organizerId = Guid.Parse(organizerClaims);

        var result = await _service.CreateGatePersonAsync(organizerId, dto);
        return Ok(result);
    }

    [HttpPut("updateScanner/{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] GatePersonCreateUpdateDto dto)
    {
        var result = await _service.UpdateGatePersonAsync(id, dto);
        return Ok(result);
    }

    [HttpDelete("deleteOrDeacticate/{id}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] bool hardDelete = false)
    {
        var success = await _service.DeleteGatePersonAsync(id, hardDelete);
        return success ? Ok() : NotFound();
    }

    [HttpGet("getAllScanners")]
    public async Task<IActionResult> GetAll()
    {
        var organizerClaims = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(organizerClaims))
        {
            return Unauthorized(new { message = " Unauthorized Please Login And Try Again." });
        }

        var organizerId = Guid.Parse(organizerClaims);
        var result = await _service.GetAllGatePersonsAsync(organizerId);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("gate-person-Information")]
    public async Task<IActionResult> GetMyDashboard()
    {

        var userClaims = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userClaims))
        {
            return Unauthorized(new { message = " Unauthorized Please Login And Try Again." });
        }

        var userId = Guid.Parse(userClaims);


        var result = await _service.GetMyDashboardAsync(userId);

        return Ok(result);
    }

}
