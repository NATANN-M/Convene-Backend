namespace Convene.Application.DTOs.Notifications
{
    public class BookingEmailDto
    {
        public Guid BookingId { get; set; }
        public string UserEmail { get; set; }
        public string UserFullName { get; set; }
        public string EventTitle { get; set; }
        public DateTime EventStartDate { get; set; }
        public string EventLocation { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
