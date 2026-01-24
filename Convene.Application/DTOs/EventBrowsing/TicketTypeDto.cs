using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.EventBrowsing
{
    public class TicketTypeDto
    {
        public Guid TicketTypeId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }   // Organizer base price
        public decimal FinalPrice { get; set; }  // Price after dynamic pricing
        public bool IsAvailable { get; set; }    // Available for booking
        public bool IsSoldOut { get; set; }      // No tickets left
    }

}
