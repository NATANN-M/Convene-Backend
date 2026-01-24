using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Domain.Enums
{
    public enum TicketStatus
    {
        Reserved = 0,   // Ticket reserved
        CheckedIn = 1,  // Scanned and verified at the gate
        Cancelled = 2   // Ticket cancelled before event
    }
}
