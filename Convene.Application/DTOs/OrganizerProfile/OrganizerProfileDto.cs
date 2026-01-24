using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.OrganizerProfile
{
    public class OrganizerProfileDto
    {
        public Guid UserId { get; set; }

        // User info
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string? ProfileImageUrl { get; set; }

        // Organizer info
        public string? BusinessName { get; set; }
        public string? BusinessEmail { get; set; }

        // Read only fields
       
        public bool IsVerified { get; set; }
        public DateTime? VerificationDate { get; set; }
        public string? AdminNotes { get; set; }

        // Dashboard metrics
        public int TotalEvents { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal EstimatedRevenue { get; set; }
    }

}
