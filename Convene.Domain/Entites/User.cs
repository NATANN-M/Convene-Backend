using Convene.Domain.Common;
using Convene.Domain.Enums;
using System;

namespace Convene.Domain.Entities
{
    public class User : BaseEntity
    {
        // Common fields
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public UserRole Role { get; set; }
        public UserStatus Status { get; set; } = UserStatus.Pending;

        public string? ProfileImageUrl { get; set; }


        // OTP fields
        public string? OtpCode { get; set; }
        public DateTime? OtpExpiration { get; set; }
        public bool? IsOtpUsed { get; set; }

        // Navigation to profile 
        public OrganizerProfile? OrganizerProfile { get; set; }
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<UserRecommendation> Recommendations { get; set; } = new List<UserRecommendation>();


    }
}
