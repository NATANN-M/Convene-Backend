using System.ComponentModel.DataAnnotations;

namespace Convene.Application.DTOs.Auth
{
    public class VerifyOtpRequestDto
    {
        [EmailAddress]
        public string Email { get; set; } = null!;

        public string OtpCode { get; set; } = null!;
    }
}
