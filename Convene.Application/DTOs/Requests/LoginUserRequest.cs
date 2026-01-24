using System.ComponentModel.DataAnnotations;

namespace Convene.Application.DTOs.Requests
{
    public class LoginUserRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;
    }
}
