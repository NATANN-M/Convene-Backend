using System.ComponentModel.DataAnnotations;

namespace Convene.Application.DTOs.Auth
{
    public class ResetPasswordDto
    {
        [EmailAddress]
        public string Email { get; set; } = null!;
        [Required]
        [Length(6, 100)]
        public string NewPassword { get; set; } = null!;
    }
}
