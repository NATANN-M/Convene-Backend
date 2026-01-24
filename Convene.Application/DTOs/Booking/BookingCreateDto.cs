using System;
using System.Collections.Generic;

namespace Convene.Application.DTOs.Booking
{
    public class BookingCreateDto
    {
        public Guid EventId { get; set; }
        

        // List of tickets (for self or others)
        public List<TicketCreateDto> Tickets { get; set; } = new List<TicketCreateDto>();
    }
}
