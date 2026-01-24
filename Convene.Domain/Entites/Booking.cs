using Convene.Domain.Common;
using Convene.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Convene.Domain.Entities
{
    public class Booking : BaseEntity
    {
        public Guid EventId { get; set; }
        public Guid UserId { get; set; } // The user 
        public BookingStatus Status { get; set; }
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public bool IsFreeEvent { get; set; }

        // Navigation
        public Event Event { get; set; }
        public User User { get; set; }

        //feedback reminder flags
        public bool FeedbackReminderSent { get; set; } = false;
        public DateTime? FeedbackReminderSentAt { get; set; }


        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();




    }
}
