using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.EventBrowsing
{
    public class EventSummaryDto
    {
        public Guid EventId { get; set; }
        public string Title { get; set; } = null!;
        public string BannerImageUrl { get; set; } = null!;
        public string Venue { get; set; } = null!;
        public DateTime TicketsaleStart { get; set; }
        public DateTime TicketsaleEnd { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal LowestTicketPrice { get; set; }
        public string CategoryName { get; set; } = null!;
        public bool IsSoldOut { get; set; } // True if all ticket types sold out
        public object ActiveBoostLevelName { get; set; }
    }

}
