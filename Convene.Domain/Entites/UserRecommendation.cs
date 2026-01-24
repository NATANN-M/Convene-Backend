using System;

namespace Convene.Domain.Entities
{
    public class UserRecommendation
    {
        public Guid UserId { get; set; }       // Owner of recommendations
        public Guid EventId { get; set; }      // Recommended event
        public float FinalScore { get; set; }  // Combined ML + Rule score
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Navigation properties 
        public User User { get; set; }
        public Event Event { get; set; }
    }
}
