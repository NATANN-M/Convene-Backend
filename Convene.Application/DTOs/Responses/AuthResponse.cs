using Convene.Domain.Enums;
using System;

namespace Convene.Application.DTOs.Responses
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;

        // JWT token 
        public string? Token { get; set; }

    
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }
        public Guid? UserId { get; set; }
     
    }
}
