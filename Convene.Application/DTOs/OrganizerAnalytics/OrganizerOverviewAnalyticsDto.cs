using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.OrganizerAnalytics
{
    public class OrganizerOverviewAnalyticsDto
    {
        public int TotalEvents { get; set; }
        public int PublishedEvents { get; set; }
        public int DraftEvents { get; set; }

        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }

        public int TotalBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int CancelledBookings { get; set; }

        public List<EventRevenueSummaryDto> EventRevenueBreakdown { get; set; } = new();
        public List<DailySalesDto> DailySales { get; set; } = new();
        public List<TopEventDto> TopEvents { get; set; } = new();
    }

    public class EventRevenueSummaryDto
    {
        public Guid EventId { get; set; }
        public string EventName { get; set; } = "";
        public decimal Revenue { get; set; }
    }

    public class DailySalesDto
    {
        public DateTime Date { get; set; }
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TopEventDto
    {
        public Guid EventId { get; set; }
        public string EventName { get; set; } = "";
        public string? CoverImage { get; set; }
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
    }
}
