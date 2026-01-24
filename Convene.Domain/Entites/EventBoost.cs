using System;

namespace Convene.Domain.Entities
{
    public class EventBoost
    {
        public Guid Id { get; set; }

        public Guid EventId { get; set; }
        public Guid OrganizerProfileId { get; set; }
        public Guid BoostLevelId { get; set; }

        public int CreditsUsed { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        // Navigation
        public Event Event { get; set; }
        public OrganizerProfile OrganizerProfile { get; set; }
        public BoostLevel BoostLevel { get; set; }
    }
}
