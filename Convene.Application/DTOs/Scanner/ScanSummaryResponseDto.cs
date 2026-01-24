namespace Convene.Application.DTOs.Scanner
{
    public class ScanSummaryResponseDto
    {
        public int TotalScans { get; set; }
        public int ValidScans { get; set; }
        public int InvalidScans { get; set; }
        public int UniqueTicketHolders { get; set; }
        public DateTime? FirstScan { get; set; }
        public DateTime? LastScan { get; set; }

        // breakdown
        public List<GatePersonSummaryDto> GatePersons { get; set; } = new();
    }

    public class GatePersonSummaryDto
    {
        public Guid GatePersonId { get; set; }
        public string GatePersonName { get; set; }
        public int TotalScans { get; set; }
        public int ValidScans { get; set; }
        public int InvalidScans { get; set; }
    }
}
