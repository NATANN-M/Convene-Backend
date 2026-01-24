using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Convene.Application.DTOs.PagenationDtos;
using Convene.Application.DTOs.Scanner;

using Convene.Application.Interfaces;

[ApiController]
[Route("api/ticket-scan")]
[Authorize(Roles = "GatePerson,Organizer,SuperAdmin")]
public class TicketScanController : ControllerBase
{
    private readonly ITicketScanService _scanService;

    public TicketScanController(ITicketScanService scanService)
    {
        _scanService = scanService;
    }

    [EnableRateLimiting("ScanRateLimit")]
    [HttpPost("scanTicketQr")]
    public async Task<IActionResult> Scan([FromBody] ScanTicketRequestDto dto)
    {
        var gatePersonClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(gatePersonClaim))
        {

            return Unauthorized(new { Message = "Unauthorized Please Login And Try Again" });
        }
        var gatePersonId = Guid.Parse(gatePersonClaim);
        var result = await _scanService.ScanAsync(dto,gatePersonId);
        return Ok(result);
    }


    [HttpGet("eventScanLogs/{eventId}")]
    public async Task<IActionResult>GetEventScanLogs(
       Guid eventId,
       [FromQuery] PagedAndSortedRequest request,
       [FromQuery] DateTime? from,
       [FromQuery] DateTime? to)
    {
        // Logged in organizer ID
        var organizerUserclaim=User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(organizerUserclaim))
        {

            return Unauthorized(new { Message = "Unauthorized Please Login And Try Again" });

        }

        var organizerUserId = Guid.Parse(organizerUserclaim);

        var logs = await _scanService.GetEventScanLogsAsync(
            organizerUserId, eventId,request, from, to);

        return Ok(logs);
    }

    [HttpGet("getLogByUserId")]
    public async Task<IActionResult>GetLogUsingId(Guid gatePersonId)
    {
        var result = await _scanService.GetScanLogByuserID(gatePersonId);

        if (result == null)
        {
            return Ok("No data Found");

        }

        return Ok(result);

    }

    [HttpGet("eventScanSummary/{eventId}")]
    public async Task<IActionResult> GetScanSummary(Guid eventId)
    {
        var organizerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(organizerIdClaim))
            return Unauthorized(new { Message = "Unauthorized. Please login again." });

        var organizerId = Guid.Parse(organizerIdClaim);

        var response = await _scanService.GetScanSummaryAsync(organizerId, eventId);
        return Ok(response);
    }

    [HttpGet("gatePerson/recent-scans")]
    public async Task<IActionResult> GetGatePersonRecentScans()
    {
        var gatePersonClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(gatePersonClaim))
            return Unauthorized(new { Message = "Unauthorized. Please login again." });

        var gatePersonId = Guid.Parse(gatePersonClaim);

        var scans = await _scanService.GetGatePersonRecentScansAsync(gatePersonId);

        return Ok(scans);
    }

}
