using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Convene.Application.DTOs.Event;
using Convene.Application.DTOs.EventBrowsing;
using Convene.Application.DTOs.PagenationDtos;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Domain.Enums;
using Convene.Infrastructure.Persistence;

namespace Convene.Infrastructure.Services
{
    public class EventBrowsingService : IEventBrowsingService
    {
        private readonly ConveneDbContext _context;
        private readonly IPricingService _pricingService;
        private readonly ITrackingService _trackingService;
        private readonly ILogger<EventBrowsingService> _logger;

        public EventBrowsingService(
            ConveneDbContext context,
            IPricingService pricingService,
            ITrackingService trackingService,
            ILogger<EventBrowsingService> logger)
        {
            _context = context;
            _pricingService = pricingService;
            _trackingService = trackingService;
            _logger = logger;
        }

        // ---------------- Active Events ----------------
        public async Task<PaginatedResult<EventSummaryDto>> GetActiveEventsAsync(PagedAndSortedRequest request)
        {
            var now = DateTime.UtcNow;

            var events = await _context.Events
                .Where(e =>
                    e.Status == EventStatus.Published &&
                    now >= e.TicketSalesStart &&
                    now <= e.TicketSalesEnd &&
                    e.EndDate > now)
                .Include(e => e.TicketTypes)
                .Include(e => e.Category)
                .ToListAsync();

            return await ApplyBoostAndPaginateAsync(events, request, now);
        }

        // ---------------- Upcoming Events ----------------
        public async Task<PaginatedResult<EventSummaryDto>> GetUpcomingEventsAsync(PagedAndSortedRequest request)
        {
            var now = DateTime.UtcNow;

            var events = await _context.Events
                .Where(e =>
                    e.Status == EventStatus.Published &&
                    e.TicketSalesStart > now &&
                    e.EndDate > now)
                .Include(e => e.TicketTypes)
                .Include(e => e.Category)
                .ToListAsync();

            return await ApplyBoostAndPaginateAsync(events, request, now);
        }

        // ---------------- Search Events ----------------
        public async Task<PaginatedResult<EventSummaryDto>> SearchEventsAsync(EventSearchRequestDto request)
        {
            var now = DateTime.UtcNow;

            var query = _context.Events
                .Where(e => e.Status == EventStatus.Published && e.EndDate > now)
                .Include(e => e.TicketTypes)
                .Include(e => e.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
                query = query.Where(e =>
                    e.Title.Contains(request.Keyword) ||
                    e.Description.Contains(request.Keyword) ||
                    e.Venue.Contains(request.Keyword));

            if (!string.IsNullOrWhiteSpace(request.CategoryName))
                query = query.Where(e => e.Category.Name.Contains(request.CategoryName));

            if (request.StartDateFrom != null)
                query = query.Where(e => e.StartDate >= request.StartDateFrom);

            if (request.StartDateTo != null)
                query = query.Where(e => e.StartDate <= request.StartDateTo);

            var events = await query.ToListAsync();

            var paginationRequest = new PagedAndSortedRequest
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            return await ApplyBoostAndPaginateAsync(events, paginationRequest, now);
        }

        // ---------------- Event Details ----------------
        public async Task<EventDetailDto> GetEventDetailsAsync(Guid eventId, Guid? userid = null)
        {
            var e = await _context.Events
                .Include(x => x.TicketTypes)
                .Include(x => x.Feedbacks)
                .FirstOrDefaultAsync(x => x.Id == eventId);

            if (e == null)
                throw new KeyNotFoundException("Event not found");

            var organizer = await _context.OrganizerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.UserId == e.OrganizerId);

            if (userid != null)
                await _trackingService.TrackInteractionAsync(userid.Value, e.Id, "view", null);

            return new EventDetailDto
            {
                EventId = e.Id,
                Title = e.Title,
                Description = e.Description,
                Venue = e.Venue,
                Category = e.Category?.Name ?? "",
                OrganizerName = organizer?.BusinessName ?? "Unknown",
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                TicketsaleStart = e.TicketSalesStart,
                TicketsaleEnd = e.TicketSalesEnd,
                Media = GetMediaFromJson(e.CoverImageUrl),
                TicketTypes = e.TicketTypes.Select(t => new TicketTypeDto
                {
                    TicketTypeId = t.Id,
                    Name = t.Name,
                    BasePrice = t.BasePrice,
                    IsAvailable = t.Quantity > 0
                }).ToList()
            };
        }

        // ---------------- BOOST + PAGINATION FIX ----------------
        private async Task<PaginatedResult<EventSummaryDto>> ApplyBoostAndPaginateAsync(
            List<Event> events,
            PagedAndSortedRequest request,
            DateTime now)
        {
            var boosts = await _context.EventBoosts
                .Include(b => b.BoostLevel)
                .Where(b => b.EndTime > now)
                .ToListAsync();

            var ordered = events
                .Select(e => new
                {
                    Event = e,
                    BoostWeight = boosts
                        .Where(b => b.EventId == e.Id)
                        .Select(b => b.BoostLevel.Weight)
                        .DefaultIfEmpty(0)
                        .Max()
                })
                .OrderByDescending(x => x.BoostWeight)
                .ThenBy(x => x.Event.StartDate)
                .ToList();

            var totalCount = ordered.Count;

            var paged = ordered
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new EventSummaryDto
                {
                    EventId = x.Event.Id,
                    Title = x.Event.Title,
                    BannerImageUrl = GetCoverImageFromJson(x.Event.CoverImageUrl),
                    Venue = x.Event.Venue,
                    StartDate = x.Event.StartDate,
                    EndDate = x.Event.EndDate,
                    CategoryName = x.Event.Category?.Name ?? "",
                    LowestTicketPrice = GetLowestTicketPrice(x.Event.TicketTypes),
                    IsSoldOut = x.Event.TicketTypes.All(t => t.Quantity <= 0)
                })
                .ToList();

            return new PaginatedResult<EventSummaryDto>
            {
                Items = paged,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }

        // ---------------- Helpers ----------------
        private string? GetCoverImageFromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<EventMediaDto>(json)?.CoverImage;
            }
            catch { return null; }
        }

        private EventMediaDto? GetMediaFromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<EventMediaDto>(json);
            }
            catch { return null; }
        }

        private decimal GetLowestTicketPrice(IEnumerable<TicketType> tickets)
        {
            var prices = tickets.Where(t => t.Quantity > 0).Select(t => t.BasePrice);
            return prices.Any() ? prices.Min() : 0;
        }
    }
}
