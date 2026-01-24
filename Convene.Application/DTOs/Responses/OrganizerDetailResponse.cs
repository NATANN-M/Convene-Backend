using Convene.Domain.Enums;

namespace Convene.Application.DTOs.Responses
{
    public class OrganizerDetailResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string BusinessEmail { get; set; } = string.Empty;
        public string KYCIdDocument { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public DateTime? VerificationDate { get; set; }
        public UserStatus Status { get; set; }
    }
}
