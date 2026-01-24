using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.GatePerson
{
    public class TicketScanLogViewDto
    {
        public DateTime ScannedAt { get; set; }
        public string? Location { get; set; }

        public string GatePersonName { get; set; } = null!;
        public string GatePersonEmail { get; set; } = null!;

        public string TicketTypeName { get; set; } = null!;
        public string TicketHolderName { get; set; } = null!;

        public string EventName { get; set; } = null!;
        public bool IsValid { get; set; }
        public string? Reason { get; set; }
    }

}
