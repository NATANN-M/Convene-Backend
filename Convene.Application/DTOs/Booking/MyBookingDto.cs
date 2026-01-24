namespace Convene.Application.DTOs.Booking
{
    public class MyBookingDto
    {
        public Guid BookingId { get; set; }
        public Guid EventId { get; set; }
        public string EventTitle { get; set; } = null!;
        public DateTime BookingDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string BookingStatus { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;

        public bool eventEnded { get; set; }
        public string? CheckoutUrl { get; set; }  // Only if payment is pending
        public bool CanPayNow { get; set; }       // true if payment is pending
        public DateTime EventEndingDate { get; set; }
    }
}
