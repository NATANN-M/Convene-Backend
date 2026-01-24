using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.Event;
using Convene.Application.DTOs.Organizer;
using Convene.Application.Interfaces;
using Convene.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

public class OrganizerDashboardService : IOrganizerDashboardService
{
    private readonly ConveneDbContext _context;

    public OrganizerDashboardService(ConveneDbContext context)
    {
        _context = context;
    }

    public async Task<List<OrganizerDashboardEventDto>> GetDashboardEventsAsync(Guid organizerId)
    {
        var events = await _context.Events
            .Where(e => e.OrganizerId == organizerId)
            .Include(e => e.Category)
            .ToListAsync();
        var result= events.Select(ev => new OrganizerDashboardEventDto
        {
            EventId = ev.Id,
            Title = ev.Title,
            BannerImageUrl = GetCoverImageFromJson(ev.CoverImageUrl),
            Description = ev.Description,
            CategoryName = ev.Category?.Name ?? "",
            Venue = ev.Venue,
            Location = ev.Location,
            StartDate = ev.StartDate,
            EndDate = ev.EndDate,
            TotalCapacity = ev.TotalCapacity,
            Status = ev.Status.ToString()
        }).ToList();

        return result;
    }

    // Helper method to extract cover image from JSON
    private string? GetCoverImageFromJson(string? coverImageUrl)
    {
        if (string.IsNullOrWhiteSpace(coverImageUrl))
            return null;

        try
        {
            var media = JsonSerializer.Deserialize<EventMediaDto>(coverImageUrl);
            return media?.CoverImage;
        }
        catch
        {
            return null;
        }
    }
}
