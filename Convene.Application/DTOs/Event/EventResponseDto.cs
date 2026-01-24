using System;
using System.Collections.Generic;

namespace Convene.Application.DTOs.Event
{
    public class EventResponseDto
    {
        public Guid EventId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string CategoryName { get; set; } = null!;
        public string? Venue { get; set; }
        public string? Location { get; set; }
        public DateTime? TicketSalesStart { get; set; }
        public DateTime? TicketSalesEnd { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalCapacity { get; set; }
        public string Status { get; set; } = "Draft";

    

        public EventMediaDto? Media { get; set; }

        public List<TicketTypeResponseDto> TicketTypes { get; set; } = new();
    }
}
