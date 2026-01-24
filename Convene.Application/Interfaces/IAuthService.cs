using Convene.Application.DTOs.Auth;
using Convene.Application.DTOs.Requests;
using Convene.Application.DTOs.Responses;
using System.Threading.Tasks;

namespace Convene.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterUserRequest request);
        Task<AuthResponse> LoginAsync(LoginUserRequest request);
        Task<AuthResponse> RegisterOrganizerAsync(RegisterOrganizerRequest request);
        Task SendOtpAsync(SendOtpRequestDto request);
        Task VerifyOtpAsync(VerifyOtpRequestDto request); 
        Task ResetPasswordAsync(ResetPasswordDto request);
    }
}
