using System.ComponentModel.DataAnnotations;

namespace Convene.Application.DTOs.Requests
{
    public class RegisterUserRequest
    {
        [Required]
        public string FullName { get; set; } = null!;
        [EmailAddress]
        public string Email { get; set; } = null!;
        [Required]
        public string PhoneNumber { get; set; } = null!;
        [MinLength(6)]
        public string Password { get; set; } = null!;
    }
}
