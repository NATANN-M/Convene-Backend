using Convene.Domain.Enums;

namespace Convene.Application.DTOs.Requests
{
    public class UserSearchRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public UserRole? Role { get; set; }
        public UserStatus? Status { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }
}
