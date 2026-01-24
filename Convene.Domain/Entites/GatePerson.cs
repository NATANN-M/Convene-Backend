using Convene.Domain.Common;
using System;

namespace Convene.Domain.Entities
{
    // GatePerson.cs - Contains ONLY GatePerson specific fields
    public class GatePerson : BaseEntity
    {
        // REFERENCE to User (for common fields/login)
        public Guid UserId { get; set; }

        // NO COMMON FIELDS HERE - they come from User table

        // GATEPERSON SPECIFIC FIELDS ONLY
        public Guid CreatedByOrganizerId { get; set; }
        public string? AssignmentsJson { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation to User (optional, for querying)
        public virtual User? User { get; set; }
    }
}
