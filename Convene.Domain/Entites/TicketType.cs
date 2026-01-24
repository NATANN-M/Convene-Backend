using Convene.Domain.Common;
using System;
using System.Collections.Generic;

namespace Convene.Domain.Entities
{
    public class TicketType : BaseEntity
    {
        public Guid EventId { get; set; }
        public string Name { get; set; } = null!;   // Regular, VIP, etc.
        public string? Description { get; set; }
        public decimal BasePrice { get; set; } = 0m; // 0 = free
        public int Quantity { get; set; } = 0;       // quantity allocated to this type
        public int Sold { get; set; } = 0;           // track sold count (booking step later)
        public bool IsActive { get; set; } = true;

        // Navigation
        public Event? Event { get; set; }
        public ICollection<DynamicPricingRule> PricingRules { get; set; } = new List<DynamicPricingRule>();
    }
}
