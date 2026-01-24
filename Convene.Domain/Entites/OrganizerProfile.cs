using Convene.Domain.Common;
using System;

namespace Convene.Domain.Entities
{
    public class OrganizerProfile : BaseEntity
    {
        //  UserId as FK
        public Guid UserId { get; set; }

        
        public string? KYCIdDocument { get; set; }
        public string? BusinessName { get; set; }
        public string? BusinessEmail { get; set; }
        public bool IsVerified { get; set; } = false;
        public DateTime? VerificationDate { get; set; }
        public string? AdminNotes { get; set; }

        // Cached rating
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }

        // Navigation back to User
        public User? User { get; set; }

        public ICollection<Event> Events { get; set; } = new List<Event>();

    }
}
