using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Convene.Application.DTOs.Requests
{
    public class RegisterOrganizerRequest
    {
        [Required]
        public string FullName { get; set; } = null!;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        public string PhoneNumber { get; set; } = null!;
        [MinLength(6)]
        public string Password { get; set; } = null!;

        
        public string BusinessName { get; set; } = null!;

        public string BusinessEmail { get; set; } = null!;

        // Accept images as IFormFile
        [Required]
        public IFormFile KYCFrontImage { get; set; } = null!;
        [Required]
        public IFormFile KYCBackImage { get; set; } = null!;
    }
}
