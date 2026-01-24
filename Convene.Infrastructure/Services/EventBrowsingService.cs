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
                .Where(e => e.Status == EventStatus.Published
                            && now >= e.TicketSalesStart
                            && now <= e.TicketSalesEnd
                            && e.EndDate > now)
                .Include(e => e.TicketTypes)
                .Include(e => e.Category);

            var pagedEvents = await baseQuery.ApplyPaginationAndSortingAsync(request);
            var events = pagedEvents.Items;

            return await ApplyBoostLogicAsync(events, now, pagedEvents);
        }

        // ---------------- Upcoming Events ----------------
        public async Task<PaginatedResult<EventSummaryDto>> GetUpcomingEventsAsync(PagedAndSortedRequest request)
        {
            var now = DateTime.UtcNow;

            var baseQuery = _context.Events
                .Where(e => e.EndDate > now && e.Status == EventStatus.Published)
                .Include(e => e.TicketTypes)
                .Include(e => e.Category);

            var pagedEvents = await baseQuery.ApplyPaginationAndSortingAsync(request);
            var events = pagedEvents.Items;

            return await ApplyBoostLogicAsync(events, now, pagedEvents);
        }

        // ---------------- Search Events ----------------
        public async Task<PaginatedResult<EventSummaryDto>> SearchEventsAsync(EventSearchRequestDto request)
        {
            var baseQuery = _context.Events
                .Where(e => e.Status == EventStatus.Published && e.EndDate > DateTime.UtcNow)
                .Include(e => e.TicketTypes)
                .Include(e => e.Category)
                .AsQueryable();

            // Apply search filters
            if (!string.IsNullOrWhiteSpace(request.Keyword))
                baseQuery = baseQuery.Where(e => e.Title.Contains(request.Keyword) || e.Description.Contains(request.Keyword) || e.Venue.Contains(request.Keyword));

            if (!string.IsNullOrWhiteSpace(request.OrganizerName))
                baseQuery = baseQuery.Where(e => _context.OrganizerProfiles.Any(o => o.Id == e.OrganizerId && o.BusinessName.Contains(request.OrganizerName)));

            if (!string.IsNullOrWhiteSpace(request.CategoryName))
                baseQuery = baseQuery.Where(e => e.Category.Name.Contains(request.CategoryName));

            if (!string.IsNullOrWhiteSpace(request.Venue))
                baseQuery = baseQuery.Where(e => e.Venue.Contains(request.Venue));

            if (request.StartDateFrom != null)
                baseQuery = baseQuery.Where(e => e.StartDate >= request.StartDateFrom);

            if (request.StartDateTo != null)
                baseQuery = baseQuery.Where(e => e.StartDate <= request.StartDateTo);

            if (request.MinPrice != null)
                baseQuery = baseQuery.Where(e => e.TicketTypes.Any(t => t.BasePrice >= request.MinPrice));

            if (request.MaxPrice != null)
                baseQuery = baseQuery.Where(e => e.TicketTypes.Any(t => t.BasePrice <= request.MaxPrice));

            // Pagination request
            var paginationRequest = new PagedAndSortedRequest
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                SortDirection = request.SortDirection
            };

            var pagedEvents = await baseQuery.ApplyPaginationAndSortingAsync(paginationRequest);
            var events = pagedEvents.Items;

            return await ApplyBoostLogicAsync(events, DateTime.UtcNow, pagedEvents);
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
                    CategoryName = _context.EventCategories.Where(c => c.Id == e.CategoryId).Select(c => c.Name).FirstOrDefault(),
                    Tickets = _context.TicketTypes.Where(tt => tt.EventId == e.Id).ToList(),
                    Feedbacks=e.Feedbacks
                })
                .FirstOrDefaultAsync();

            if (eventResult == null) throw new KeyNotFoundException("Event not found.");

            var organizerProfile = await _context.OrganizerProfiles
     .AsNoTracking() // because we only need to read cached values
     .FirstOrDefaultAsync(op => op.UserId == eventResult.OrganizerId);

            string businessName = organizerProfile?.BusinessName ?? "Unknown or Not Defined Organizer";
            Guid organizerId = organizerProfile?.UserId ?? Guid.Empty;
            double averageRating = organizerProfile?.AverageRating ?? 0.0;
            int totalRating = organizerProfile?.TotalRatings ?? 0;


            EventMediaDto? media = null;
            if (!string.IsNullOrEmpty(eventResult.Event.CoverImageUrl))
            {
                try { media = JsonSerializer.Deserialize<EventMediaDto>(eventResult.Event.CoverImageUrl); } catch { media = null; }
            }

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
            else
            {
                _logger.LogInformation($"User not signed in. Event view not tracked: {eventResult.Event.Title}");
            }

            return new EventDetailDto
            {
                EventId = eventResult.Event.Id,
                OrganizerId = organizerId,
                Title = eventResult.Event.Title,
                Description = eventResult.Event.Description ?? eventResult.Event.Title,
                Venue = eventResult.Event.Venue ?? eventResult.Event.Title,
                Category = eventResult.CategoryName ?? "Uncategorized",
                Location = eventResult.Event.Location ?? eventResult.Event.Title,
                OrganizerName = businessName,
                OrganizerAverageRating = averageRating,
                OrganizerTotalRatings = totalRating,
                TicketsaleStart = eventResult.Event.TicketSalesStart,
                TicketsaleEnd = eventResult.Event.TicketSalesEnd,
                StartDate = eventResult.Event.StartDate,
                EndDate = eventResult.Event.EndDate,
                LowestTicketPrice = GetLowestTicketPrice(eventResult.Tickets),
                TicketTypes = ticketDtos,
                Media = media
            };
        }

        // ---------------------- Helper Methods ----------------------
        private async Task<PaginatedResult<EventSummaryDto>> ApplyBoostLogicAsync(IEnumerable<Event> events, DateTime now, PaginatedResult<Event> pagedEvents)
        {
            var boosts = await _context.EventBoosts
                .Include(b => b.BoostLevel)
                .Where(b => events.Select(e => e.Id).Contains(b.EventId) && b.EndTime > now)
                .ToListAsync();

            var dtoList = events.Select(e =>
            {
                var media = GetCoverImageFromJson(e.CoverImageUrl);
                var activeBoostName = boosts
                    .Where(b => b.EventId == e.Id)
                    .OrderByDescending(b => b.BoostLevel.Weight)
                    .Select(b => b.BoostLevel.Name)
                    .FirstOrDefault() ?? "";

                return new EventSummaryDto
                {
                    EventId = e.Id,
                    Title = e.Title,
                    BannerImageUrl = media ?? "Banner_Image Undefined",
                    Venue = e.Venue ?? "Unspecified See The Location",
                    TicketsaleStart = e.TicketSalesStart,
                    TicketsaleEnd = e.TicketSalesEnd,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    CategoryName = e.Category?.Name ?? "",
                    ActiveBoostLevelName = activeBoostName,
                    LowestTicketPrice = GetLowestTicketPrice(e.TicketTypes),
                    IsSoldOut = e.TicketTypes.All(t => t.Quantity <= 0)
                };
            }).ToList();

            // Gold/Premium first
            var goldPremium = dtoList
                .Where(d => boosts.Any(b => b.EventId == d.EventId && (b.BoostLevel.Name == "Gold" || b.BoostLevel.Name == "Premium")))
                .OrderBy(x => Guid.NewGuid())
                .Take(5)
                .ToList();

            // Other boosts
            var otherBoosts = dtoList
                .Where(d => boosts.Any(b => b.EventId == d.EventId && b.BoostLevel.Name != "Gold" && b.BoostLevel.Name != "Premium" && !goldPremium.Contains(d)))
                .OrderBy(x => Guid.NewGuid())
                .ToList();

            // Normal events
            var normalEvents = dtoList
                .Where(d => !boosts.Any(b => b.EventId == d.EventId))
                .ToList();

            var finalList = goldPremium.Concat(otherBoosts).Concat(normalEvents).ToList();

            return new PaginatedResult<EventSummaryDto>
            {
                Items = finalList,
                TotalCount = pagedEvents.TotalCount,
                PageNumber = pagedEvents.PageNumber,
                PageSize = pagedEvents.PageSize,
                TotalPages = pagedEvents.TotalPages
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

        private decimal GetLowestTicketPrice(IEnumerable<TicketType> ticketTypes)
        {
            var availableTypes = ticketTypes.Where(t => t.Quantity > 0).Select(t => t.BasePrice);
            return availableTypes.Any() ? availableTypes.Min() : 0;
        }
    }
}
