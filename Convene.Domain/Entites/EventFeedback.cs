using Convene.Domain.Common; 
using System;

namespace Convene.Domain.Entities
{
    public class EventFeedback : BaseEntity
    {
        public Guid EventId { get; set; }        
        public Guid UserId { get; set; }         // The user who submitted the feedback
        public int Rating { get; set; }          // Rating (1-5)
        public string? Comment { get; set; }     
        // Navigation properties
        public Event Event { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
