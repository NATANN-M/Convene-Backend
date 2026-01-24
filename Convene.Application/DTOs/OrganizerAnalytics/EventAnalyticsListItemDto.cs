using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.OrganizerAnalytics
{
    public class EventAnalyticsListItemDto
    {
        public Guid EventId { get; set; }
        public string Title { get; set; } = "";
        public string? CoverImage { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "";

        public int TicketsSold { get; set; }
        public int TotalTickets { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
