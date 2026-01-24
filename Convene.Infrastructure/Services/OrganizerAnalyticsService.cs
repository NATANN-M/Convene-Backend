using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.OrganizerAnalytics;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Domain.Enums;
using Convene.Infrastructure.Persistence;
using System;
using QuestPDF.Fluent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Services
{
    public class OrganizerAnalyticsService : IOrganizerAnalyticsService
    {
        private readonly ConveneDbContext _context;
        private readonly IPricingService _pricingService;

        public OrganizerAnalyticsService(ConveneDbContext context, IPricingService pricingService)
        {
            _context = context;
            _pricingService = pricingService;
        }

        /// <summary>
        /// Overview KPIs across all events for the organizer.
        /// </summary>
        public async Task<OrganizerOverviewAnalyticsDto> GetOverviewAnalyticsAsync(Guid organizerId)
        {
            var events = await _context.Events
                .Where(e => e.OrganizerId == organizerId)
                .AsNoTracking()
                .ToListAsync();

            int totalEvents = events.Count;
            int publishedEvents = events.Count(e => e.Status == EventStatus.Published);
            int draftEvents = totalEvents - publishedEvents;

            // Tickets sold and revenue
            var soldStatuses = new[] { TicketStatus.Reserved };

            var ticketsQuery = _context.Tickets
                .Where(t => t.Event.OrganizerId == organizerId)
                .AsNoTracking();

            int totalTicketsSold = await ticketsQuery.CountAsync(t => soldStatuses.Contains(t.Status));

            decimal totalRevenue = await ticketsQuery
                .Where(t => soldStatuses.Contains(t.Status))
                .SumAsync(t => (decimal?)t.Price) ?? 0m;

            // Bookings
            var bookingsQuery = _context.Bookings
                .Where(b => b.Event.OrganizerId == organizerId)
                .AsNoTracking();

            int totalBookings = await bookingsQuery.CountAsync();
            int confirmedBookings = await bookingsQuery.CountAsync(b => b.Status == BookingStatus.Confirmed);
            int cancelledBookings = await bookingsQuery.CountAsync(b => b.Status == BookingStatus.Cancelled);

            // Event revenue breakdown
            var eventRevenue = await _context.Tickets
                .Where(t => t.Event.OrganizerId == organizerId && soldStatuses.Contains(t.Status))
                .GroupBy(t => new { t.EventId, t.Event.Title })
                .Select(g => new EventRevenueSummaryDto
                {
                    EventId = g.Key.EventId,
                    EventName = g.Key.Title,
                    Revenue = g.Sum(t => t.Price)
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            // Daily sales last 30 days
            var since = DateTime.UtcNow.Date.AddDays(-29);
            var dailySalesRaw = await _context.Tickets
                .Where(t => t.Event.OrganizerId == organizerId
                            && t.Booking != null
                            && t.Booking.BookingDate >= since
                            && soldStatuses.Contains(t.Status))
                .GroupBy(t => t.Booking.BookingDate.Date)
                .Select(g => new DailySalesDto
                {
                    Date = g.Key,
                    TicketsSold = g.Count(),
                    Revenue = g.Sum(t => t.Price)
                })
                .ToListAsync();

            // Fill missing days
            var dailySales = new List<DailySalesDto>();
            for (int i = 0; i < 30; i++)
            {
                var day = since.AddDays(i);
                var existing = dailySalesRaw.FirstOrDefault(d => d.Date == day);
                dailySales.Add(existing ?? new DailySalesDto { Date = day, TicketsSold = 0, Revenue = 0m });
            }

            // Top 5 events
            var topEvents = eventRevenue
                .Take(5)
                .Select(x => new TopEventDto
                {
                    EventId = x.EventId,
                    EventName = x.EventName,
                    CoverImage = GetCoverImageForEvent(x.EventId),
                    TicketsSold = _context.Tickets
                        .Where(t => t.EventId == x.EventId && soldStatuses.Contains(t.Status))
                        .Count(),
                    Revenue = x.Revenue
                })
                .ToList();

            return new OrganizerOverviewAnalyticsDto
            {
                TotalEvents = totalEvents,
                PublishedEvents = publishedEvents,
                DraftEvents = draftEvents,
                TotalTicketsSold = totalTicketsSold,
                TotalRevenue = totalRevenue,
                TotalBookings = totalBookings,
                ConfirmedBookings = confirmedBookings,
                CancelledBookings = cancelledBookings,
                EventRevenueBreakdown = eventRevenue,
                DailySales = dailySales,
                TopEvents = topEvents
            };
        }

        /// <summary>
        /// Short list of events for organizer analytics page
        /// </summary>
        public async Task<List<EventAnalyticsListItemDto>> GetOrganizerAnalyticsEventsAsync(Guid organizerId)
        {
            var soldStatuses = new[] { TicketStatus.Reserved };

            var events = await _context.Events
                .Where(e => e.OrganizerId == organizerId)
                .Include(e => e.TicketTypes)
                .AsNoTracking()
                .ToListAsync();

            var result = new List<EventAnalyticsListItemDto>();

            foreach (var ev in events)
            {
                int ticketsSold = await _context.Tickets
                    .Where(t => t.EventId == ev.Id && soldStatuses.Contains(t.Status))
                    .CountAsync();

                int totalTickets = ev.TicketTypes.Sum(tt => tt.Quantity);

                decimal revenue = await _context.Tickets
                    .Where(t => t.EventId == ev.Id && soldStatuses.Contains(t.Status))
                    .SumAsync(t => (decimal?)t.Price) ?? 0m;

                result.Add(new EventAnalyticsListItemDto
                {
                    EventId = ev.Id,
                    Title = ev.Title,
                    CoverImage = ExtractCoverImage(ev.CoverImageUrl),
                    StartDate = ev.StartDate,
                    EndDate = ev.EndDate,
                    Status = ev.Status.ToString(),
                    TicketsSold = ticketsSold,
                    TotalTickets = totalTickets,
                    TotalRevenue = revenue
                });
            }

            return result;
        }

        /// <summary>
        /// Detailed analytics for a single event
        /// </summary>
        public async Task<EventAnalyticsDto> GetEventAnalyticsAsync(Guid organizerId, Guid eventId)
        {
            var ev = await _context.Events
                .Where(e => e.Id == eventId && e.OrganizerId == organizerId)
                .Include(e => e.TicketTypes)
                .ThenInclude(t => t.PricingRules)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (ev == null)
                throw new KeyNotFoundException("Event not found or access denied");

            var soldStatuses = new[] { TicketStatus.Reserved,TicketStatus.CheckedIn};

            int totalTicketsSold = await _context.Tickets
                .Where(t => t.EventId == eventId && soldStatuses.Contains(t.Status))
                .CountAsync();

            int totalCapacity = ev.TicketTypes.Sum(tt => tt.Quantity);

            decimal totalRevenue = await _context.Tickets
                .Where(t => t.EventId == eventId && soldStatuses.Contains(t.Status))
                .SumAsync(t => (decimal?)t.Price) ?? 0m;

            // Ticket analytics
            var ticketAnalytics = new List<EventTicketAnalyticsDto>();
            foreach (var tt in ev.TicketTypes)
            {
                int soldCount = await _context.Tickets
                    .Where(t => t.EventId == eventId && t.TicketTypeId == tt.Id && soldStatuses.Contains(t.Status))
                    .CountAsync();

                decimal revenue = await _context.Tickets
                    .Where(t => t.EventId == eventId && t.TicketTypeId == tt.Id && soldStatuses.Contains(t.Status))
                    .SumAsync(t => (decimal?)t.Price) ?? 0m;

                decimal currentPrice = await _pricingService.GetCurrentPriceAsync(tt.Id);

                string? appliedRuleName = tt.PricingRules.FirstOrDefault(r => r.IsActive)?.RuleType.ToString();

                ticketAnalytics.Add(new EventTicketAnalyticsDto
                {
                    TicketTypeId = tt.Id,
                    TicketName = tt.Name,
                    TotalAvailable = tt.Quantity,
                    Sold = soldCount,
                    CurrentPrice = currentPrice,
                    Revenue = revenue,
                    AppliedPricingRuleName = appliedRuleName
                });
            }

            // Booking counts
            var bookings = _context.Bookings.Where(b => b.EventId == eventId);
            int pending = await bookings.CountAsync(b => b.Status == BookingStatus.Pending);
            int confirmed = await bookings.CountAsync(b => b.Status == BookingStatus.Confirmed);
            int cancelled = await bookings.CountAsync(b => b.Status == BookingStatus.Cancelled);

            // Daily sales last 30 days
            var since = DateTime.UtcNow.Date.AddDays(-29);
            var dailyRaw = await _context.Tickets
                .Where(t => t.EventId == eventId
                            && t.Booking != null
                            && t.Booking.BookingDate >= since
                            && soldStatuses.Contains(t.Status))
                .GroupBy(t => t.Booking.BookingDate.Date)
                .Select(g => new DailySalesDto
                {
                    Date = g.Key,
                    TicketsSold = g.Count(),
                    Revenue = g.Sum(t => t.Price)
                })
                .ToListAsync();

            var dailySales = new List<DailySalesDto>();
            for (int i = 0; i < 30; i++)
            {
                var day = since.AddDays(i);
                var row = dailyRaw.FirstOrDefault(d => d.Date == day);
                dailySales.Add(row ?? new DailySalesDto { Date = day, TicketsSold = 0, Revenue = 0m });
            }

            // Last 7 days
            var since7 = DateTime.UtcNow.Date.AddDays(-6);
            var last7 = dailySales.Where(d => d.Date >= since7).OrderBy(d => d.Date).ToList();

            return new EventAnalyticsDto
            {
                EventId = ev.Id,
                Title = ev.Title,
                CoverImage = ExtractCoverImage(ev.CoverImageUrl),
                StartDate = ev.StartDate,
                EndDate = ev.EndDate,
                Status = ev.Status.ToString(),
                TotalTicketsSold = totalTicketsSold,
                RemainingTickets = totalCapacity - totalTicketsSold,
                TotalRevenue = totalRevenue,
                Tickets = ticketAnalytics,
                DailyRevenue = dailySales,
                Last7DaysSales = last7,
                PendingBookings = pending,
                ConfirmedBookings = confirmed,
                CancelledBookings = cancelled
            };
        }

        private string? ExtractCoverImage(string? coverImageJson)
        {
            if (string.IsNullOrEmpty(coverImageJson)) return null;
            try
            {
                using var doc = JsonDocument.Parse(coverImageJson);
                if (doc.RootElement.TryGetProperty("CoverImage", out var el) && el.ValueKind == JsonValueKind.String)
                    return el.GetString();
            }
            catch
            {
                return null;
            }
            return null;
        }

        private string? GetCoverImageForEvent(Guid eventId)
        {
            var ev = _context.Events.AsNoTracking().FirstOrDefault(e => e.Id == eventId);
            return ev == null ? null : ExtractCoverImage(ev.CoverImageUrl);
        }


        public async Task<List<OrganizerBookedUserDto>> GetBookedUsersAsync(
      Guid organizerId,
      Guid eventId,
      DateTime? startDate,
      DateTime? endDate,
      Guid? ticketTypeId)
        {
            // Validate event owner
            var ev = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == organizerId);

            if (ev == null)
                throw new KeyNotFoundException("Event not found or access denied.");

            // Base query
            var bookingsQuery = _context.Bookings
                .Where(b => b.EventId == eventId && b.Status == BookingStatus.Confirmed)
                .Include(b => b.User)
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.TicketType)
                .AsQueryable();

            // Filter by date range
            if (startDate.HasValue)
                bookingsQuery = bookingsQuery.Where(b => b.BookingDate >= startDate.Value);

            if (endDate.HasValue)
                bookingsQuery = bookingsQuery.Where(b => b.BookingDate <= endDate.Value);

            // Filter by ticket type
            if (ticketTypeId.HasValue)
                bookingsQuery = bookingsQuery.Where(b => b.Tickets.Any(t => t.TicketTypeId == ticketTypeId.Value));

            var confirmedBookings = await bookingsQuery.ToListAsync();

            var result = new List<OrganizerBookedUserDto>();

            foreach (var booking in confirmedBookings)
            {
                var userDto = new OrganizerBookedUserDto
                {
                    UserId = booking.UserId,
                    FullName = booking.User.FullName,
                    Email = booking.User.Email,
                    PhoneNumber = booking.User.PhoneNumber,
                    TotalSpent = booking.TotalAmount,
                    Tickets = booking.Tickets.Select(t =>
                    {
                        return new UserTicketDto
                        {
                            TicketId = t.Id,
                            TicketTypeName = t.TicketType.Name,
                            Price = t.Price,
                            Quantity = 1,
                            AppliedPricingRuleName = t.TicketType.Name,
                            PurchaseDate = booking.Payments.FirstOrDefault()?.PaidAt ??booking.BookingDate
                        };
                    }).ToList()
                };

                result.Add(userDto);
            }

            return result;
        }



        public async Task<ExportFileDto> ExportBookedUsersAsync(
    Guid organizerId,
    Guid eventId,
    DateTime? startDate,
    DateTime? endDate,
    Guid? ticketTypeId,
    string format)
        {
            var users = await GetBookedUsersAsync(
                organizerId,
                eventId,
                startDate,
                endDate,
                ticketTypeId);

            return format.ToLower() switch
            {
                "pdf" => GenerateBookedUsersPdf(users),
                _ => GenerateBookedUsersCsv(users)
            };
        }
        #region PDF Generation Placeholder

        private ExportFileDto GenerateBookedUsersCsv(List<OrganizerBookedUserDto> users)
        {
            var sb = new StringBuilder();

            // Add UTF-8 BOM for Excel compatibility
            sb.Append('\uFEFF');

            // Headers
            sb.AppendLine("Full Name,Email,Phone Number,Ticket Type,Quantity,Unit Price,Total Amount,Applied Pricing Rule,Purchase Date");

            foreach (var user in users)
            {
                foreach (var ticket in user.Tickets)
                {
                    var totalAmount = ticket.Quantity * ticket.Price;
                    var purchaseDate = ticket.PurchaseDate.ToString("yyyy-MM-dd HH:mm");

                    // Escape fields that might contain commas
                    sb.AppendLine($"\"{user.FullName}\",\"{user.Email}\",\"{user.PhoneNumber}\",\"{ticket.TicketTypeName}\",{ticket.Quantity},{ticket.Price:F2},{totalAmount:F2},\"{ticket.AppliedPricingRuleName}\",\"{purchaseDate}\"");
                }
            }

            return new ExportFileDto
            {
                Bytes = Encoding.UTF8.GetBytes(sb.ToString()),
                ContentType = "text/csv; charset=utf-8",
                FileName = $"event-booked-users-{DateTime.Now:yyyyMMdd-HHmmss}.csv"
            };
        }

        private ExportFileDto GenerateBookedUsersPdf(List<OrganizerBookedUserDto> users)
        {
            var bytes = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);

                    page.Header().AlignCenter().Text("Event Booked Users Report")
                        .FontSize(20).Bold().FontColor(QuestPDF.Helpers.Colors.Blue.Medium);

                    page.Content().PaddingVertical(20).Column(column =>
                    {
                        // Summary information
                        column.Item().Text($"Total Users: {users.Count}")
                            .FontSize(12).SemiBold();

                        column.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // User
                                columns.RelativeColumn(2); // Contact
                                columns.RelativeColumn(2); // Ticket Info
                                columns.RelativeColumn(1); // Quantity
                                columns.RelativeColumn(1); // Total
                            });

                            // Table header
                            table.Header(header =>
                            {
                                header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(5).Text("User");
                                header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(5).Text("Contact");
                                header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(5).Text("Ticket Info");
                                header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(5).Text("Qty");
                                header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(5).Text("Total");
                            });

                            // Table rows
                            foreach (var user in users)
                            {
                                foreach (var ticket in user.Tickets)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text(user.FullName);
                                    table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text($"{user.Email}\n{user.PhoneNumber}");
                                    table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text($"{ticket.TicketTypeName}\n${ticket.Price:0.00} each");
                                    table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text(ticket.Quantity.ToString());
                                    table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text($"${(ticket.Quantity * ticket.Price):0.00}");
                                }
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Generated on: ").FontSize(10);
                        text.Span($"{DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(10).SemiBold();
                    });
                });
            }).GeneratePdf();

            return new ExportFileDto
            {
                Bytes = bytes,
                ContentType = "application/pdf",
                FileName = $"event-booked-users-{DateTime.Now:yyyyMMdd-HHmmss}.pdf"
            };
        }


        #endregion

    }
}
