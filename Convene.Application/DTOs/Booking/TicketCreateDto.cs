using System;

namespace Convene.Application.DTOs.Booking
{
    public class TicketCreateDto
    {
        public Guid TicketTypeId { get; set; }

        // For the logged-in user, these can be empty or auto-filled
        public string? HolderName { get; set; }
        public string? HolderPhone { get; set; }

        // Number of tickets to create for this holder (optional for simplicity)
        public int Quantity { get; set; } = 1;
    }
}
