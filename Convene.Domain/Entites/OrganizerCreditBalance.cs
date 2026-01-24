using System;

namespace Convene.Domain.Entities
{
    public class OrganizerCreditBalance
    {
        public Guid Id { get; set; }
        public Guid OrganizerProfileId { get; set; }
        public Guid UserId { get; set; }  // organizer
        public int Balance { get; set; }

        public DateTime LastUpdated { get; set; }

        // Navigation
        public OrganizerProfile OrganizerProfile { get; set; }
    }
}
