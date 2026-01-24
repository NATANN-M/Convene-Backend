using System;

namespace Convene.Application.DTOs.Recommendation
{
    public class TrackInteractionDto
    {
                    
        public Guid EventId { get; set; }            // Event being interacted with
        public string InteractionType { get; set; } = null!; // View / Book / Search / Favorite / Share
        public string? UserLocation { get; set; }    // Optional: "lat,lng"
    }
}
