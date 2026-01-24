using System;

namespace Convene.Application.DTOs.Booking
{
    public class TicketViewDto
    {
        public Guid TicketId { get; set; }
        public string TicketTypeName { get; set; } = null!;
        public string HolderName { get; set; } = null!;
        public string HolderPhone { get; set; } = null!;
        public string QrCode { get; set; } = null!;
        public decimal Price { get; set; }
        public string Status { get; set; } = null!;
    }
}
