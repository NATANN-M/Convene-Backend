using System;
using System.Collections.Generic;

namespace Convene.Application.DTOs.Booking
{
    public class BookingResultDto
    {
        public Guid BookingId { get; set; }
        public string Message { get; set; } = "Booking created successfully.";
        public DateTime BookingDate { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsFree { get; set; }
        public List<TicketViewDto> Tickets { get; set; } = new List<TicketViewDto>();
    }
}
