using System;

namespace Convene.Domain.Entities
{
    public class BoostLevel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public int CreditCost { get; set; }
        public int DurationHours { get; set; }
        public int Weight { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
