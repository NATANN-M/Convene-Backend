using System;
using System.Collections.Generic;

namespace Convene.Application.DTOs.Booking
{
    public class BookingSummaryDto
    {
        public Guid BookingId { get; set; }
        public string EventTitle { get; set; } = null!;
        public DateTime BookingDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
        public bool IsFreeEvent { get; set; }

        public List<TicketViewDto> Tickets { get; set; } = new List<TicketViewDto>();
    }
}
