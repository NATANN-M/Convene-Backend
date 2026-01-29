using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.Event;
using Convene.Application.DTOs.PagenationDtos;
using Convene.Application.DTOs.Requests;
using Convene.Application.DTOs.Responses;
using Convene.Application.Interfaces;
using Convene.Infrastructure.Persistence;
using Convene.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Convene.Domain.Enums;


namespace Convene.API.Controllers
{

    [Route("api/admin")]
    [ApiController]
  [Authorize(Roles = "SuperAdmin,Admin")] // Only admins
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ConveneDbContext _context;
        private readonly IPricingService _pricingService;


        public AdminController(IAdminService adminService, ConveneDbContext context,IPricingService pricingService)
        {
            _adminService = adminService;
            _context = context;
            _pricingService = pricingService;
        }



        [HttpGet("test-auth")]
        [AllowAnonymous]
        public IActionResult TestAuth()
        {
            var userClaims = new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated,
                UserName = User.Identity?.Name,
                Roles = User.Claims.Where(c => c.Type == "role" || c.Type == ClaimTypes.Role).Select(c => c.Value).ToList(),
                AllClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
            };

            return Ok(userClaims);
        }

        // Organizer Management Endpoints
        [HttpGet("organizers/get-pending")]
        public async Task<IActionResult> GetPendingOrganizers([FromQuery] PagedAndSortedRequest request)
        {
            var result = await _adminService.GetPendingOrganizersAsync(request);
            return Ok(result);
        }


        [HttpGet("organizers/get-all")]
        public async Task<IActionResult> GetAllOrganizers(
     [FromQuery] string? status,
     [FromQuery] PagedAndSortedRequest request)
        {
            var result = await _adminService.GetAllOrganizersAsync(request, status);
            return Ok(result);
        }


        [HttpGet("organizers/get-by-id/{id}")]
        public async Task<IActionResult> GetOrganizerById(Guid id)
        {
            var organizer = await _adminService.GetOrganizerDetailAsync(id);
            if (organizer == null) return NotFound("Organizer not found.");
            return Ok(organizer);
        }

        [HttpPost("organizers/approve/{id}")]
        public async Task<IActionResult> ApproveOrganizer(Guid id, [FromBody] OrganizerApprovalRequest? request)
        {
            var result = await _adminService.ApproveOrganizerAsync(id, request?.AdminNotes);
            if (!result) return NotFound(new { Message = "Organizer not found" });
            return Ok(new { Message = "Organizer approved successfully" });
        }

        [HttpPost("organizers/reject/{id}")]
        public async Task<IActionResult> RejectOrganizer(Guid id, [FromBody] OrganizerApprovalRequest? request)
        {
            var result = await _adminService.RejectOrganizerAsync(id, request?.AdminNotes);
            if (!result) return NotFound(new { Message = "Organizer not found" });
            return Ok(new { Message = "Organizer rejected successfully" });
        }

        // User Management Endpoints
        [HttpGet("users/get-all")]
        public async Task<IActionResult> GetAllUsers(
     [FromQuery] string? role,
     [FromQuery] string? status,
     [FromQuery] PagedAndSortedRequest request)
        {
            var result = await _adminService.GetAllUsersAsync(request, role, status);
            return Ok(result);
        }


        [HttpGet("users/search")]
        public async Task<IActionResult> SearchUsers(
     [FromQuery] UserSearchRequest search,
     [FromQuery] PagedAndSortedRequest request)
        {
            var result = await _adminService.SearchUsersAsync(request, search);
            return Ok(result);
        }


        [HttpPost("users/update-status")]
        public async Task<IActionResult> UpdateUserStatus([FromBody] UpdateUserStatusRequest request)
        {
            var result = await _adminService.UpdateUserStatusAsync(request);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("events")]
        public async Task<ActionResult<List<EventResponseDto>>> GetAllEvents()
        {
            try
            {
                // Step 1: Load events with collections using split query to prevent cartesian explosion
                var events = await _context.Events
                    .Include(e => e.TicketTypes)
                        .ThenInclude(t => t.PricingRules)
                    .Include(e => e.Category)
                    .AsSplitQuery() // split queries for multiple collections
                                    // ORDER BY: Published first, then newest updated/created first
                    .OrderByDescending(e => e.Status == EventStatus.Published)
                    .ThenByDescending(e => e.UpdatedAt ?? e.CreatedAt)
                    .ToListAsync();

                var result = new List<EventResponseDto>();

                foreach (var ev in events)
                {
                    EventMediaDto? media = null;
                    if (!string.IsNullOrEmpty(ev.CoverImageUrl))
                    {
                        try
                        {
                            media = System.Text.Json.JsonSerializer.Deserialize<EventMediaDto>(ev.CoverImageUrl);
                        }
                        catch
                        {
                            media = null;
                        }
                    }

                    var ticketDtos = new List<TicketTypeResponseDto>();
                    foreach (var t in ev.TicketTypes)
                    {
                        var currentPrice = await _pricingService.GetCurrentPriceAsync(t.Id);

                        var pricingRulesDto = t.PricingRules.Select(r => new PricingRuleResponseDto
                        {
                            Id = r.Id,
                            RuleType = r.RuleType,
                            Description = r.Description,
                            DiscountPercent = r.DiscountPercent,
                            PriceIncreasePercent = r.PriceIncreasePercent,
                            ThresholdPercentage = r.ThresholdPercentage,
                            LastNDaysBeforeEvent = r.LastNDaysBeforeEvent,
                            StartDate = r.StartDate,
                            EndDate = r.EndDate,
                            IsActive = r.IsActive
                        }).ToList();

                        ticketDtos.Add(new TicketTypeResponseDto
                        {
                            Id = t.Id,
                            Name = t.Name,
                            Description = t.Description,
                            BasePrice = t.BasePrice,
                            CurrentPrice = currentPrice,
                            Quantity = t.Quantity,
                            Sold = t.Sold,
                            PricingRules = pricingRulesDto
                        });
                    }

                    // Step 4: Add the event DTO to the result
                    result.Add(new EventResponseDto
                    {
                        EventId = ev.Id,
                        Title = ev.Title,
                        Description = ev.Description,
                        CategoryName = ev.Category?.Name ?? "Uncategorized",
                        Venue = ev.Venue,
                        Location = ev.Location,
                        StartDate = ev.StartDate,
                        EndDate = ev.EndDate,
                        TotalCapacity = ev.TotalCapacity,
                        Status = ev.Status.ToString(),
                        Media = media,
                        TicketTypes = ticketDtos
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving events",
                    error = ex.Message
                });
            }
        }

    }
}
