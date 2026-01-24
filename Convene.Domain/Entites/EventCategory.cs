using Convene.Domain.Common;
using System;
using System.Collections.Generic;

namespace Convene.Domain.Entities
{
    public class EventCategory : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }         // UI icon if needed
        public bool IsDefault { get; set; } = false; // seeded defaults
        public string? CreatedBy { get; set; }       // "System" or admin email/id

        // Navigation
        public ICollection<Event>? Events { get; set; }
    }
}
