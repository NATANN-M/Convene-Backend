using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.Auth;
using Convene.Application.DTOs.Requests;
using Convene.Application.DTOs.Responses;
using Convene.Application.Interfaces;
using Convene.Application.Interfaces.Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Domain.Enums;
using Convene.Infrastructure.Helpers;
using Convene.Infrastructure.Persistence;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly ConveneDbContext _context;
        private readonly IEmailService _emailService;
        private readonly PasswordHasher _passwordHasher;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly JwtService _jwtService;

        public AuthService(ConveneDbContext context, PasswordHasher passwordHasher, JwtService jwtService, IEmailService emailService, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _emailService = emailService;
           _cloudinaryService = cloudinaryService;
        }

        // Attendee registration
        public async Task<AuthResponse> RegisterAsync(RegisterUserRequest request)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
                return new AuthResponse { Success = false, Message = "Email already exists" };

            var cleanPhoneNumber = PhoneNumberHelper.NormalizeEthiopianPhone(request.PhoneNumber);

            if (string.IsNullOrEmpty(cleanPhoneNumber))
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid phone number format"
                };

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = cleanPhoneNumber,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                Role = UserRole.Attendee,
                Status = UserStatus.Active
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            string token = _jwtService.GenerateToken(user);

            return new AuthResponse
            {
                Success = true,
                Message = "Attendee registered successfully",
                Token = token,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                Status = user.Status.ToString()
            };
        }

        // Organizer registration
        public async Task<AuthResponse> RegisterOrganizerAsync(RegisterOrganizerRequest request)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
                return new AuthResponse { Success = false, Message = "Email already exists" };


            var cleanPhoneNumber = PhoneNumberHelper.NormalizeEthiopianPhone(request.PhoneNumber);

            if (string.IsNullOrEmpty(cleanPhoneNumber))
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid phone number format"
                };

            

            var frontImage = await _cloudinaryService.UploadImageAsync(request.KYCFrontImage,"Kyc-documents");
            var backImage = await _cloudinaryService.UploadImageAsync(request.KYCBackImage, "Kyc_documents");

            if(string.IsNullOrEmpty(frontImage) || string.IsNullOrEmpty(backImage) )
            {
                return new AuthResponse { Status = "false", Message = "Failed to upload kyc document try again" };
            }

            var kycdata = new
            {
                FrontImage = frontImage,
                BackImage = backImage

            };

            var kycjson = JsonSerializer.Serialize(kycdata);

            // Create User
            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = cleanPhoneNumber,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                Role = UserRole.Organizer,
                Status = UserStatus.Pending
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Save KYC paths in OrganizerProfile
            var profile = new OrganizerProfile
            {
                UserId = user.Id,
                BusinessName = request.BusinessName,
                BusinessEmail = request.BusinessEmail,
                KYCIdDocument = kycjson // store both as json
            };
            _context.OrganizerProfiles.Add(profile);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                Success = true,
                Message = "Organizer registered successfully, pending admin approval",
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                Status = user.Status.ToString()
            };
        }



        // Login for all
        public async Task<AuthResponse> LoginAsync(LoginUserRequest request)
        {

            var email=request.Email.Trim().ToLower();
            var password = request.Password.Trim();
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return new AuthResponse { Success = false, Message = "Email and password are required" };
            }


            var user = await _context.Users
        .Include(u => u.OrganizerProfile)
        .FirstOrDefaultAsync(u => u.Email.ToLower() ==email);

            if (user == null)
                return new AuthResponse { Success = false, Message = "Invalid email or password" };

            bool validPassword = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
            if (!validPassword)
                return new AuthResponse { Success = false, Message = "Invalid email or password" };

            if ((user.Role == UserRole.Attendee && user.Status != UserStatus.Active)){

                return new AuthResponse { Success = false, Message = "User Accound Bloked/Deactivated By Admin For more Info Contact Support" };
            }

            // Only allow login if attendee is active OR organizer is approved
           
              if (user.Role == UserRole.Organizer && (!user.OrganizerProfile.IsVerified || user.Status != UserStatus.Active))
            {
                return new AuthResponse { Success = false, Message = "Organizer Account not authorized to login yet Or Blocked Contact Support" };
            }

            string token = _jwtService.GenerateToken(user);

            return new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                Status = user.Status.ToString()
            };
        }

       
        public async Task SendOtpAsync(SendOtpRequestDto request)
        {
             var email = request.Email.Trim().ToLower();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user == null)
                throw new KeyNotFoundException("No user found with this email.");

         
            var otp = new Random().Next(100000, 999999).ToString();

            user.OtpCode = otp;
            user.OtpExpiration = DateTime.UtcNow.AddMinutes(5);
            user.IsOtpUsed = false;

            await _context.SaveChangesAsync();

            var body = $"<p>Your Convene verification code is <strong>{otp}</strong>. It will expire in 5 minutes.</p>";
            await _emailService.SendEmailAsync(request.Email, "Convene Password Reset Code", body);
        }

        
        public async Task VerifyOtpAsync(VerifyOtpRequestDto request)
        {
            var email = request.Email.Trim().ToLower();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user == null)
                throw new KeyNotFoundException("Invalid email.");

            if (user.OtpCode != request.OtpCode)
                throw new KeyNotFoundException("Invalid OTP code.");

            if (user.OtpExpiration < DateTime.UtcNow)
                throw new KeyNotFoundException("OTP has expired.");

            if (user.IsOtpUsed == true)
                throw new KeyNotFoundException("OTP already used.");

            
            user.IsOtpUsed = true;
            await _context.SaveChangesAsync();
        }

     
        public async Task ResetPasswordAsync(ResetPasswordDto request)
        {
            var email = request.Email.Trim().ToLower();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            if (user.IsOtpUsed != true)
                throw new KeyNotFoundException("OTP verification required before resetting password.");

            
            var hashedPassword = _passwordHasher.HashPassword(request.NewPassword);
            user.PasswordHash = hashedPassword;

            // Clear OTP info
            user.OtpCode = null;
            user.OtpExpiration = null;
            user.IsOtpUsed = null;

            await _context.SaveChangesAsync();
        }

    }
}
