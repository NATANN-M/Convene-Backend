using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.Event
{
    public class TicketTypeCreateDto
    {
        public string Name { get; set; } = null!; // Regular, VIP, etc.
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public int Quantity { get; set; }

        // for pricing rules
        // public List<PricingRuleCreateDto>? PricingRules { get; set; } = new();
    }
}
