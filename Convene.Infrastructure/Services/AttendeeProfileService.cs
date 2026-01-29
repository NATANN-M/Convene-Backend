using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.AttendeeProfile;
using Convene.Application.Interfaces;
using Convene.Application.Interfaces.Convene.Application.Interfaces;
using Convene.Domain.Enums;
using Convene.Infrastructure.Persistence;

namespace Convene.Infrastructure.Services
{
    public class AttendeeProfileService : IAttendeeProfileService
    {
        private readonly ConveneDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;

        public AttendeeProfileService(ConveneDbContext context, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<AttendeeProfileDto> GetProfileAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.Bookings)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new KeyNotFoundException("User not found");

            var confirmed = user.Bookings.Count(b => b.Status == BookingStatus.Confirmed);

            var totalSpent = await _context.Payments
                .Where(p => p.Booking.UserId == userId && p.Status==PaymentStatus.Paid)
                .SumAsync(p => p.Amount);

            return new AttendeeProfileDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl,

                TotalBookings = user.Bookings.Count,
                ConfirmedBookings = confirmed,
                TotalAmountSpent = totalSpent
            };
        }

        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateAttendeeProfileDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new KeyNotFoundException("User not found");

            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> UpdateProfileImageAsync(Guid userId, IFormFile file)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new KeyNotFoundException("User not found");

            // Remove old profile image
            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
                await _cloudinaryService.DeleteFileAsync(user.ProfileImageUrl);

            // Upload new image
            var imageUrl = await _cloudinaryService.UploadImageAsync(file, "profiles");

            user.ProfileImageUrl = imageUrl;
            await _context.SaveChangesAsync();

            return imageUrl;
        }
    }

}
