using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Domain.Enums
{
    public enum BookingStatus
    {
        Pending = 0,     // Created but not paied
        Confirmed = 1,   // Success
        Cancelled = 2    // User or system cancelled booking
    }
}

