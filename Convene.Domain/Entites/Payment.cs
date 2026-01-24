using Convene.Domain.Common;
using Convene.Domain.Enums;
using System;

namespace Convene.Domain.Entities
{
    public class Payment : BaseEntity
    {
        // Existing booking payment
        public Guid? BookingId { get; set; }
        public Booking? Booking { get; set; }

        // New: Credit purchase
        public Guid? OrganizerProfileId { get; set; }
        public OrganizerProfile? OrganizerProfile { get; set; }

        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public string PaymentReference { get; set; } = null!;
        public string? ChapaCheckoutUrl { get; set; }

        // Payer info
        public string PayerName { get; set; } = null!;
        public string PayerEmail { get; set; } = null!;
        public string PayerPhone { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }

        // Reminder tracking
        public bool ReminderSent { get; set; } = false;
        public DateTime? ReminderSentAt { get; set; }
    }
}
