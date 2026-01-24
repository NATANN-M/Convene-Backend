using System.ComponentModel.DataAnnotations;

namespace Convene.Application.DTOs.Auth
{
    public class SendOtpRequestDto
    {
        [EmailAddress]
        public string Email { get; set; } = null!;
    }
}
