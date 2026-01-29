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

            var pagedEvents = await baseQuery.ApplyPaginationAndSortingAsync(request);
            return await ApplyBoostLogicAsync(pagedEvents.Items, now, pagedEvents);
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

            var pagedEvents = await baseQuery.ApplyPaginationAndSortingAsync(request);
            return await ApplyBoostLogicAsync(pagedEvents.Items, now, pagedEvents);
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

            var pagedEvents = await baseQuery.ApplyPaginationAndSortingAsync(paginationRequest);
            return await ApplyBoostLogicAsync(pagedEvents.Items, now, pagedEvents);
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

            var media = GetCoverImageFromJson(eventResult.Event.CoverImageUrl);

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
                Media = media == null ? null : new EventMediaDto { CoverImage = media }
            };
        }

        // ---------------------- Helper Methods ----------------------
        private async Task<PaginatedResult<EventSummaryDto>> ApplyBoostLogicAsync(
            IEnumerable<Event> events,
            DateTime now,
            PaginatedResult<Event> pagedEvents)
        {
            var boosts = await _context.EventBoosts
                .Include(b => b.BoostLevel)
                .Where(b => events.Select(e => e.Id).Contains(b.EventId) && b.EndTime > now)
                .ToListAsync();

            var dtoList = events.Select(e =>
            {
                var activeBoost = boosts
                    .Where(b => b.EventId == e.Id)
                    .OrderByDescending(b => b.BoostLevel.Weight)
                    .ThenBy(_ => Guid.NewGuid())
                    .FirstOrDefault();

                return new EventSummaryDto
                {
                    EventId = e.Id,
                    Title = e.Title,
                    BannerImageUrl = GetCoverImageFromJson(e.CoverImageUrl),
                    Venue = e.Venue,
                    TicketsaleStart = e.TicketSalesStart,
                    TicketsaleEnd = e.TicketSalesEnd,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    CategoryName = e.Category?.Name ?? "",
                    ActiveBoostLevelName = activeBoost?.BoostLevel.Name ?? "",
                    LowestTicketPrice = GetLowestTicketPrice(e.TicketTypes),
                    IsSoldOut = e.TicketTypes.All(t => t.Quantity <= 0)
                };
            }).ToList();

            return new PaginatedResult<EventSummaryDto>
            {
                Items = dtoList,
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
            var availableTypes = ticketTypes
                .Where(t => t.Quantity > 0)
                .Select(t => t.BasePrice);

            return availableTypes.Any() ? availableTypes.Min() : 0;
        }
    }
}
