using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.AdminPlatformSetting;
using Convene.Application.DTOs.PagenationDtos;
using Convene.Application.DTOs.Requests;
using Convene.Application.DTOs.Responses;
using Convene.Application.EmailTemplates;
using Convene.Application.Interfaces;
using Convene.Domain.Enums;
using Convene.Infrastructure.Common;
using Convene.Infrastructure.Persistence;

namespace Convene.Infrastructure.Services
{
    public class AdminService : IAdminService
    {
        private readonly ConveneDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ICreditService _creditService;

        public AdminService(ConveneDbContext context,
            IEmailService emailService,
            ICreditService creditService)
        {
            _context = context;
            _emailService = emailService;
            _creditService = creditService;
        }

        // Paginated pending organizers
        public async Task<PaginatedResult<OrganizerPendingResponse>> GetPendingOrganizersAsync(PagedAndSortedRequest request)
        {
            var query = _context.Users
                .Where(u => u.Role == UserRole.Organizer && u.Status == UserStatus.Pending)
                .Select(u => new OrganizerPendingResponse
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    BusinessName = u.OrganizerProfile.BusinessName! ?? null,
                    BusinessEmail = u.OrganizerProfile.BusinessEmail!,
                    KYCIdDocument = u.OrganizerProfile.KYCIdDocument
                });

            return await query.ApplyPaginationAndSortingAsync(request);
        }

        // Paginated all users
        public async Task<PaginatedResult<UserListResponse>> GetAllUsersAsync(PagedAndSortedRequest request, string? role = null, string? status = null)
        {
            var query = _context.Users
         .Where(u => u.Role != UserRole.SuperAdmin) // Exclude super admins
         .AsQueryable();

            if (!string.IsNullOrEmpty(role) && Enum.TryParse<UserRole>(role, true, out var parsedRole))
                query = query.Where(u => u.Role == parsedRole);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<UserStatus>(status, true, out var parsedStatus))
                query = query.Where(u => u.Status == parsedStatus);

            var projected = query.Select(u => new UserListResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role.ToString(),
                Status = u.Status.ToString(),
                CreatedAt = u.CreatedAt
            });

            return await projected.ApplyPaginationAndSortingAsync(request);
        }

        // Paginated all organizers
        public async Task<PaginatedResult<UserListResponse>> GetAllOrganizersAsync(PagedAndSortedRequest request, string? status = null)
        {
            var query = _context.Users
                .Where(u => u.Role == UserRole.Organizer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<UserStatus>(status, true, out var parsedStatus))
                query = query.Where(u => u.Status == parsedStatus);

            var projected = query.Select(u => new UserListResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role.ToString(),
                Status = u.Status.ToString(),
                CreatedAt = u.CreatedAt
            });

            return await projected.ApplyPaginationAndSortingAsync(request);
        }

        // Paginated search users
        public async Task<PaginatedResult<UserListResponse>> SearchUsersAsync(PagedAndSortedRequest request, UserSearchRequest search)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search.FullName))
                query = query.Where(u => u.FullName.Contains(search.FullName));

            if (!string.IsNullOrEmpty(search.Email))
                query = query.Where(u => u.Email.Contains(search.Email));

            if (!string.IsNullOrEmpty(search.PhoneNumber))
                query = query.Where(u => u.PhoneNumber.Contains(search.PhoneNumber));

            if (search.Role.HasValue)
                query = query.Where(u => u.Role == search.Role.Value);

            if (search.Status.HasValue)
                query = query.Where(u => u.Status == search.Status.Value);

            if (search.CreatedFrom.HasValue)
                query = query.Where(u => u.CreatedAt >= search.CreatedFrom.Value);

            if (search.CreatedTo.HasValue)
                query = query.Where(u => u.CreatedAt <= search.CreatedTo.Value);

            var projected = query.Select(u => new UserListResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role.ToString(),
                Status = u.Status.ToString(),
                CreatedAt = u.CreatedAt
            });

            return await projected.ApplyPaginationAndSortingAsync(request);
        }

        // Get organizer detail
        public async Task<OrganizerDetailResponse?> GetOrganizerDetailAsync(Guid organizerId)
        {
            var organizer = await _context.Users
                .Where(u => u.Id == organizerId && u.Role == UserRole.Organizer)
                .Select(u => new OrganizerDetailResponse
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    BusinessName = u.OrganizerProfile.BusinessName!,
                    BusinessEmail = u.OrganizerProfile.BusinessEmail!,
                    KYCIdDocument = u.OrganizerProfile.KYCIdDocument!,
                    IsVerified = u.OrganizerProfile.IsVerified,
                    VerificationDate = u.OrganizerProfile.VerificationDate,
                    Status = u.Status
                })
                .FirstOrDefaultAsync();

            return organizer;
        }

        // Approve organizer
        public async Task<bool> ApproveOrganizerAsync(Guid organizerId, string? adminNotes = null)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = await _context.Users
                    .Include(u => u.OrganizerProfile)
                    .FirstOrDefaultAsync(u => u.Id == organizerId && u.Role == UserRole.Organizer);
                if (user == null) return false;

                user.Status = UserStatus.Active;
                user.OrganizerProfile.IsVerified = true;
                if (!string.IsNullOrEmpty(adminNotes))
                    user.OrganizerProfile.AdminNotes = adminNotes;
                user.OrganizerProfile.VerificationDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var settings = await _context.PlatformSettings.FirstAsync();

                var request = new AdminAddCreditDto
                {
                    UserId = organizerId,
                    Credits = settings.InitialOrganizerCredits,
                    Reason = "Initial Credit"
                };


                await _creditService.AdminAddCreditsAsync(request);

                await transaction.CommitAsync();

                try
                {
                    var htmlBody = ApproveOrRejectOrganizerEmailTemplate.ApprovetOrganizerEmailTemplate();

                    await _emailService.SendEmailAsync(
                        user.Email,
                        "Organizer Approval From Convene",
                        htmlBody
                    );
                }
                catch
                {
                    Console.WriteLine("Failed to send approval email to organizer.");
                }

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        // Reject organizer
        public async Task<bool> RejectOrganizerAsync(Guid organizerId, string? adminNotes = null)
        {
            var user = await _context.Users
                .Include(u => u.OrganizerProfile)
                .FirstOrDefaultAsync(u => u.Id == organizerId && u.Role == UserRole.Organizer);

            if (user == null) return false;

            user.Status = UserStatus.Inactive;
            user.OrganizerProfile.IsVerified = false;
            if (!string.IsNullOrEmpty(adminNotes))
                user.OrganizerProfile.AdminNotes = adminNotes;
            user.OrganizerProfile.VerificationDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Generate the HTML email content
            var htmlContent = ApproveOrRejectOrganizerEmailTemplate.RejectOrganizerEmailTemplate(adminNotes ?? "No specific reason provided.");
            var emailSubject = "Organizer Application Status Update - Convene";

            // Send the HTML email
            await _emailService.SendEmailAsync(
                toEmail: user.Email,
                subject: emailSubject,
                htmlContent: htmlContent);

            return true;
        }

        // Update user status
        public async Task<AuthResponse> UpdateUserStatusAsync(UpdateUserStatusRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user == null)
                return new AuthResponse { Success = false, Message = "User not found" };

            if (user.Role == UserRole.SuperAdmin)
            {

                return new AuthResponse { Success = false, Message = "You Can Not Block Super Admin!" };
            }

            user.Status = request.IsActive ? UserStatus.Active : UserStatus.Inactive;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var userStatus = (user.Status).ToString();
            var htmlcontent = $"<p><strong>Dear {user.Email} Your Convene Account Has Been {userStatus}</strong>";

            await _emailService.SendEmailAsync(user.Email, "Status Updated", htmlcontent);

            return new AuthResponse
            {
                Success = true,
                Message = $"User status updated to {(request.IsActive ? "Active" : "Inactive")}",
                // UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                Status = user.Status.ToString()
            };
        }
    }
}
