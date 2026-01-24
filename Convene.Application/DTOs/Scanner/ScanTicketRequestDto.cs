using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.Scanner
{
    public class ScanTicketRequestDto
    {
        public string QrCode { get; set; } = null!;
       // public Guid ScannerUserId { get; set; }       // The logged-in gateperson / organizer / admin user
        public string? DeviceId { get; set; }
        public string? Location { get; set; }
    }

}
