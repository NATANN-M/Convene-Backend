using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.OrganizerProfile;
using Convene.Application.Interfaces;
using Convene.Application.Interfaces.Convene.Application.Interfaces;
using Convene.Infrastructure.Persistence;

namespace Convene.Infrastructure.Services
{
    public class OrganizerProfileService : IOrganizerProfileService
    {
        private readonly ConveneDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;

        public OrganizerProfileService(
            ConveneDbContext context,
            ICloudinaryService cloudinaryService)
        {
            _context = context;
           _cloudinaryService=cloudinaryService;
        }

        public async Task<OrganizerProfileDto> GetProfileAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.OrganizerProfile)
                .Include(u => u.OrganizerProfile.Events)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new Exception("User not found");

            var profile = user.OrganizerProfile;

            var totalTicketsSold = await _context.Tickets
                .Where(t => t.Event.OrganizerId == profile.Id)
                .CountAsync();

            var estimatedRevenue = await _context.Tickets
                .Where(t => t.Event.OrganizerId == profile.Id)
                .SumAsync(t => t.Price);  //TODO: must be sold so i must update latter

            return new OrganizerProfileDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl,

                BusinessName = profile?.BusinessName,
                BusinessEmail = profile?.BusinessEmail,
             //   KYCIdDocument = profile?.KYCIdDocument,
                IsVerified = profile?.IsVerified ?? false,
                VerificationDate = profile?.VerificationDate,
                AdminNotes = profile?.AdminNotes,

                TotalEvents = profile?.Events.Count ?? 0,
                TotalTicketsSold = totalTicketsSold,
                EstimatedRevenue = estimatedRevenue
            };
        }

        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateOrganizerProfileDto dto)
        {
            var user = await _context.Users
                .Include(u => u.OrganizerProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new Exception("User not found");

            var profile = user.OrganizerProfile;
            if (profile == null)
                throw new Exception("Organizer profile not found");

            // Update allowed fields
            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;

            profile.BusinessName = dto.BusinessName;
            profile.BusinessEmail = dto.BusinessEmail;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<string> UpdateProfileImageAsync(Guid userId, IFormFile file)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new Exception("User not found");

            // Delete old image if exists
            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
                await _cloudinaryService.DeleteFileAsync(user.ProfileImageUrl);

            // Upload new image
            var newImageUrl = await _cloudinaryService.UploadImageAsync(file, "profiles");

            user.ProfileImageUrl = newImageUrl;
            await _context.SaveChangesAsync();

            return newImageUrl;
        }
    }

}
