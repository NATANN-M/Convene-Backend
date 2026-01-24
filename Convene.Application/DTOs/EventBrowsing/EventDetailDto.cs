using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convene.Application.DTOs.Event;

namespace Convene.Application.DTOs.EventBrowsing
{
    public class EventDetailDto
    {
        public Guid EventId { get; set; }
        public Guid OrganizerId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string Venue { get; set; } = null!;
        public string? Location { get; set; }
        public string OrganizerName { get; set; } = null!;
        public double OrganizerAverageRating { get; set; } = 0;
        public int OrganizerTotalRatings { get; set; } = 0;
        public DateTime TicketsaleStart { get; set; }
        public DateTime TicketsaleEnd { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal LowestTicketPrice { get; set; }
        public EventMediaDto? Media { get; set; }

        public List<TicketTypeDto> TicketTypes { get; set; } = new();
     
    }


}
