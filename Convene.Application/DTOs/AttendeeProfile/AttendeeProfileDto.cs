using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.AttendeeProfile
{
    public class AttendeeProfileDto
    {
        public Guid UserId { get; set; }

        // Basic info
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string? ProfileImageUrl { get; set; }

        // Analytics
        public int TotalBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public decimal TotalAmountSpent { get; set; }
    }

}
