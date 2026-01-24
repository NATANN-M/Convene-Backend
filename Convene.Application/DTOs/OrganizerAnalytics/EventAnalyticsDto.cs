using System;
using System.Collections.Generic;
using Convene.Application.DTOs.OrganizerAnalytics;

namespace Convene.Application.DTOs.OrganizerAnalytics
{
    public class EventAnalyticsDto
    {
        // Event summary
        public Guid EventId { get; set; }
        public string Title { get; set; } = "";
        public string? CoverImage { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "";

        // overall numbers
        public int TotalTicketsSold { get; set; }
        public int RemainingTickets { get; set; }
        public decimal TotalRevenue { get; set; }

        // ticket breakdown
        public List<EventTicketAnalyticsDto> Tickets { get; set; } = new();

        // charts
        public List<DailySalesDto> DailyRevenue { get; set; } = new();
        public List<DailySalesDto> Last7DaysSales { get; set; } = new();

        // bookings
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int CancelledBookings { get; set; }
    }

    public class EventTicketAnalyticsDto
    {
        public Guid TicketTypeId { get; set; }
        public string TicketName { get; set; } = "";
        public int TotalAvailable { get; set; }
        public int Sold { get; set; }
        public int Remaining => TotalAvailable - Sold;
        public decimal CurrentPrice { get; set; }
        public decimal Revenue { get; set; }

        // pricing rule info (name or null)
        public string? AppliedPricingRuleName { get; set; }
    }
}
