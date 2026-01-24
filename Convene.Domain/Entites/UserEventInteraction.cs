using System;

namespace Convene.Domain.Entities
{
    public class UserEventInteraction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }       // FK to user
        public Guid EventId { get; set; }      // FK to event
        public string InteractionType { get; set; } = null!; // View, Book, Search, Favorite, Share
        public string? UserLocation { get; set; }           // "lat,lng"
        public string Category { get; set; } = null!;      // Cached event category
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation properties 
        // public User User { get; set; }
        // public Event Event { get; set; }
    }
}
