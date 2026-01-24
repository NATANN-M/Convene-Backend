namespace Convene.Application.DTOs.Scanner
{
    public class GatePersonScanHistoryDto
    {
        public Guid Id { get; set; }
        public DateTime ScannedAt { get; set; }

        public string TicketHolderName { get; set; }
        public string TicketHolderEmail { get; set; }

        public string TicketTypeName { get; set; }

        public bool IsValid { get; set; }
        public string Reason { get; set; }

        public string EventName { get; set; }
    }
}
