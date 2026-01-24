namespace Convene.Application.DTOs.Event

{
    public class EventTelegramDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Venue { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal? LowestTicketPrice { get; set; }

        // Media
        public List<string> ImageUrls { get; set; } = new();
        public List<string> VideoUrls { get; set; } = new();
    }
}
