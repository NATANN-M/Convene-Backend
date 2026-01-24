using Convene.Domain.Common;
using System;

namespace Convene.Domain.Entities
{
    public class TicketScanLog : BaseEntity
    {
        public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
        public string? Location { get; set; }
        public string? DeviceId { get; set; }

        // Scanner Snapshot (GatePerson / Organizer / Admin)
        public Guid? ScannerUserId { get; set; }
        public string? ScannerName { get; set; }
        public string? ScannerEmail { get; set; }

        // Ticket Snapshot
        public Guid TicketId { get; set; }
        public string TicketTypeName { get; set; } = null!;
        public string TicketHolderName { get; set; } = null!;
        public string TicketHolderEmail { get; set; } = null!;

        // Event Snapshot
        public Guid EventId { get; set; }
        public string EventName { get; set; } = null!;
        public DateTime EventStart { get; set; }
        public DateTime EventEnd { get; set; }

        // Validation
        public bool IsValid { get; set; }
        public string? Reason { get; set; }
    }
}
