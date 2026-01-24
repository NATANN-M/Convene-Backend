using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.Scanner
{
    public class TicketScanLogResponseDto
    {
        public Guid Id { get; set; }

        public DateTime ScannedAt { get; set; }

        // Scanner
        public Guid? ScannerUserId { get; set; }
        public string ScannerName { get; set; }
        public string ScannerEmail { get; set; }
        public string ScannerRole { get; set; }

        // Ticket
        public string TicketTypeName { get; set; }
        public string TicketHolderName { get; set; }
        public string TicketHolderEmail { get; set; }

        // Event
        public Guid EventId { get; set; }
        public string EventName { get; set; }

        // Metadata
        public string Location { get; set; }
        public string DeviceId { get; set; }
        public DateTime EventStart { get; set; }
        public DateTime EventEnd { get; set; }
        public bool IsValid { get; set; }
        public string? Reason { get; set; }
    }

}
