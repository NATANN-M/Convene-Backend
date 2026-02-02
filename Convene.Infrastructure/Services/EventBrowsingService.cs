using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Convene.Application.DTOs.Event;
using Convene.Application.DTOs.EventBrowsing;
using Convene.Application.DTOs.OrganizerProfile;
using Convene.Application.DTOs.PagenationDtos;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Domain.Enums;
using Convene.Infrastructure.Common;
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

            var baseQuery = _context.Events
                .Where(e =>
                    e.Status == EventStatus.Published &&
                    now >= e.TicketSalesStart &&
                    now <= e.TicketSalesEnd &&
                    e.EndDate > now)
                .Include(e => e.TicketTypes)
                .Include(e => e.Category)
                .OrderBy(e => e.StartDate); // stable ordering

            return await ApplyBoostLogicAsync(baseQuery, request, now);


        }

        // ---------------- Upcoming Events ----------------
        public async Task<PaginatedResult<EventSummaryDto>> GetUpcomingEventsAsync(PagedAndSortedRequest request)
        {
            var now = DateTime.UtcNow;

            var baseQuery = _context.Events
                .Where(e =>
                    e.Status == EventStatus.Published &&
                    e.TicketSalesStart > now &&
                    e.EndDate > now)
                .Include(e => e.TicketTypes)
                .Include(e => e.Category)
                .OrderBy(e => e.TicketSalesStart);

            return await ApplyBoostLogicAsync(baseQuery, request, now);

        }

        // ---------------- Search Events ----------------
        public async Task<PaginatedResult<EventSummaryDto>> SearchEventsAsync(EventSearchRequestDto request)
        {
            var now = DateTime.UtcNow;

            var baseQuery = _context.Events
                .Where(e => e.Status == EventStatus.Published && e.EndDate > now)
                .Include(e => e.TicketTypes)
                .Include(e => e.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
                baseQuery = baseQuery.Where(e =>
                    e.Title.Contains(request.Keyword) ||
                    e.Description.Contains(request.Keyword) ||
                    e.Venue.Contains(request.Keyword));

            if (!string.IsNullOrWhiteSpace(request.OrganizerName))
                baseQuery = baseQuery.Where(e =>
                    _context.OrganizerProfiles.Any(o =>
                        o.Id == e.OrganizerId &&
                        o.BusinessName.Contains(request.OrganizerName)));

            if (!string.IsNullOrWhiteSpace(request.CategoryName))
                baseQuery = baseQuery.Where(e => e.Category.Name.Contains(request.CategoryName));

            if (!string.IsNullOrWhiteSpace(request.Venue))
                baseQuery = baseQuery.Where(e => e.Venue.Contains(request.Venue));

            if (request.StartDateFrom != null)
                baseQuery = baseQuery.Where(e => e.StartDate >= request.StartDateFrom);

            if (request.StartDateTo != null)
                baseQuery = baseQuery.Where(e => e.StartDate <= request.StartDateTo);

            if (request.MinPrice != null)
                baseQuery = baseQuery.Where(e =>
                    e.TicketTypes.Any(t => t.Quantity > 0 && t.BasePrice >= request.MinPrice));

            if (request.MaxPrice != null)
                baseQuery = baseQuery.Where(e =>
                    e.TicketTypes.Any(t => t.Quantity > 0 && t.BasePrice <= request.MaxPrice));

            baseQuery = baseQuery.OrderBy(e => e.StartDate);

            var paginationRequest = new PagedAndSortedRequest
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                SortDirection = request.SortDirection
            };

            return await ApplyBoostLogicAsync(baseQuery, request, now);

        }

        // ---------------- Event Details ----------------
        public async Task<EventDetailDto> GetEventDetailsAsync(Guid eventId, Guid? userid = null)
        {
            var eventResult = await _context.Events
                .Where(e => e.Id == eventId)
                .Select(e => new
                {
                    Event = e,
                    e.OrganizerId,
                    CategoryName = _context.EventCategories
                        .Where(c => c.Id == e.CategoryId)
                        .Select(c => c.Name)
                        .FirstOrDefault(),
                    Tickets = _context.TicketTypes
                        .Where(tt => tt.EventId == e.Id)
                        .ToList(),
                    Feedbacks = e.Feedbacks
                })
                .FirstOrDefaultAsync();

            if (eventResult == null)
                throw new KeyNotFoundException("Event not found.");

            var organizerProfile = await _context.OrganizerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(op => op.UserId == eventResult.OrganizerId);

            var media = GetMediaFromJson(eventResult.Event.CoverImageUrl); // CHANGED HERE

            var ticketDtos = new List<TicketTypeDto>();
            foreach (var t in eventResult.Tickets)
            {
                var finalPrice = await _pricingService.GetCurrentPriceAsync(t.Id);
                ticketDtos.Add(new TicketTypeDto
                {
                    TicketTypeId = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    BasePrice = t.BasePrice,
                    FinalPrice = finalPrice,
                    IsAvailable = t.Quantity > 0 && t.IsActive,
                    IsSoldOut = t.Quantity <= 0
                });
            }

            if (userid != null)
            {
                await _trackingService.TrackInteractionAsync(userid.Value, eventResult.Event.Id, "view", null);
                _logger.LogInformation($"Tracked view interaction for user {userid.Value} on event {eventResult.Event.Id}");
            }

            return new EventDetailDto
            {
                EventId = eventResult.Event.Id,
                OrganizerId = organizerProfile?.UserId ?? Guid.Empty,
                Title = eventResult.Event.Title,
                Description = eventResult.Event.Description ?? eventResult.Event.Title,
                Venue = eventResult.Event.Venue ?? eventResult.Event.Title,
                Category = eventResult.CategoryName ?? "Uncategorized",
                Location = eventResult.Event.Location ?? eventResult.Event.Title,
                OrganizerName = organizerProfile?.BusinessName ?? "Unknown",
                OrganizerAverageRating = organizerProfile?.AverageRating ?? 0,
                OrganizerTotalRatings = organizerProfile?.TotalRatings ?? 0,
                TicketsaleStart = eventResult.Event.TicketSalesStart,
                TicketsaleEnd = eventResult.Event.TicketSalesEnd,
                StartDate = eventResult.Event.StartDate,
                EndDate = eventResult.Event.EndDate,
                LowestTicketPrice = GetLowestTicketPrice(eventResult.Tickets),
                TicketTypes = ticketDtos,
                Media = media // CHANGED HERE - directly assign the EventMediaDto
            };
        }

        // ---------------------- Helper Methods ----------------------
        private async Task<PaginatedResult<EventSummaryDto>> ApplyBoostLogicAsync(
     IQueryable<Event> baseQuery,
     PagedAndSortedRequest request,
     DateTime now)
        {
            // 1️⃣ Load events first (NO pagination yet)
            var events = await baseQuery.ToListAsync();

            // 2️⃣ Load active boosts
            var boosts = await _context.EventBoosts
                .Include(b => b.BoostLevel)
                .Where(b => b.EndTime > now)
                .ToListAsync();

            // 3️⃣ Apply boost ordering FIRST
            var orderedEvents = events
                .Select(e => new
                {
                    Event = e,
                    ActiveBoost = boosts
                        .Where(b => b.EventId == e.Id)
                        .OrderByDescending(b => b.BoostLevel.Weight)
                        .FirstOrDefault()
                })
                .OrderByDescending(x => x.ActiveBoost != null ? x.ActiveBoost.BoostLevel.Weight : 0)
                .ThenBy(x => x.Event.StartDate)
                .ToList();

            // 4️⃣ Pagination AFTER boost
            var totalCount = orderedEvents.Count;

            var paged = orderedEvents
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // 5️⃣ Map DTOs (ALL fields preserved)
            var dtoList = paged.Select(x => new EventSummaryDto
            {
                EventId = x.Event.Id,
                Title = x.Event.Title,
                BannerImageUrl = GetCoverImageFromJson(x.Event.CoverImageUrl),
                Venue = x.Event.Venue,
                TicketsaleStart = x.Event.TicketSalesStart,
                TicketsaleEnd = x.Event.TicketSalesEnd,
                StartDate = x.Event.StartDate,
                EndDate = x.Event.EndDate,
                CategoryName = x.Event.Category?.Name ?? "",
                ActiveBoostLevelName = x.ActiveBoost?.BoostLevel.Name ?? "",
                LowestTicketPrice = GetLowestTicketPrice(x.Event.TicketTypes),
                IsSoldOut = x.Event.TicketTypes.All(t => t.Quantity <= 0)
            }).ToList();

            return new PaginatedResult<EventSummaryDto>
            {
                Items = dtoList,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }


        private string? GetCoverImageFromJson(string? coverImageUrl)
        {
            if (string.IsNullOrWhiteSpace(coverImageUrl)) return null;
            try
            {
                var media = JsonSerializer.Deserialize<EventMediaDto>(coverImageUrl);
                return media?.CoverImage;
            }
            catch { return null; }
        }


        // NEW: For EventDetailDto (needs full EventMediaDto)
        private EventMediaDto? GetMediaFromJson(string? coverImageUrl)
        {
            if (string.IsNullOrWhiteSpace(coverImageUrl)) return null;
            try
            {
                return JsonSerializer.Deserialize<EventMediaDto>(coverImageUrl);
            }
            catch { return null; }
        }

        private decimal GetLowestTicketPrice(IEnumerable<TicketType> ticketTypes)
        {
            var availableTypes = ticketTypes
                .Where(t => t.Quantity > 0)
                .Select(t => t.BasePrice);

            return availableTypes.Any() ? availableTypes.Min() : 0;
        }
    }
}
