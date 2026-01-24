using System;

namespace Convene.Domain.Entities
{
    public class PlatformSettings
    {
        public Guid Id { get; set; }

        public int InitialOrganizerCredits { get; set; }
        public int EventPublishCost { get; set; }
        public decimal CreditPriceETB { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
