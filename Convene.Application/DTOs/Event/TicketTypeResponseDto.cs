using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.Event
{
    public class TicketTypeResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public decimal CurrentPrice { get; set; }    // newly added
        public int Quantity { get; set; }
        public int Sold { get; set; }
        public bool IsActive { get; set; }
        public List<PricingRuleResponseDto> PricingRules { get; set; } = new();
        public string Description { get; set; }
    }

}
