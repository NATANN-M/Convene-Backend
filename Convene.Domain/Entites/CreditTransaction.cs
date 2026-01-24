using System;
using Convene.Domain.Enums;

namespace Convene.Domain.Entities
{
    public class CreditTransaction
    {
        public Guid Id { get; set; }

        public Guid OrganizerProfileId { get; set; }
        public OrganizerProfile OrganizerProfile { get; set; }
        public Guid UserId {get; set;} //for querying
        // Number of credits bought or deducted
        public int CreditsChanged { get; set; }

        // Purchase | PublishEvent | BoostEvent
        public string Type { get; set; }

        public string? Description { get; set; }

        
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public string? PaymentReference { get; set; } // Used to match callback
        public string? ChapaCheckoutUrl { get; set; }

     
        public decimal? TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}
