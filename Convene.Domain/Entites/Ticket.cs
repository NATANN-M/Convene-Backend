using Convene.Domain.Common;
using Convene.Domain.Enums;
using System;

namespace Convene.Domain.Entities
{
    public class Ticket : BaseEntity
    {
        public Guid BookingId { get; set; }
        public Guid TicketTypeId { get; set; }
        public Guid EventId { get; set; }

        public string HolderName { get; set; } // Optional: if booking for others
        public string HolderPhone { get; set; } // Required if booking for others
        public string QrCode { get; set; } // 

        public decimal Price { get; set; }
        public TicketStatus Status { get; set; } = TicketStatus.Reserved;

        // Navigation
        public Booking Booking { get; set; }
        public TicketType TicketType { get; set; }
        public Event Event { get; set; }
    }
}
