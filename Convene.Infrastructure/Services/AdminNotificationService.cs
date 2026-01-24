using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.AdminNotifications;
using Convene.Application.Interfaces;
using Convene.Domain.Enums;
using Convene.Infrastructure.Persistence;

namespace Convene.Infrastructure.Services
{
    public class AdminNotificationService : IAdminNotificationService
    {
        private readonly ConveneDbContext _context;
        private readonly INotificationService _notificationService;

        public AdminNotificationService(
            ConveneDbContext context,
            INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

       
        public async Task SendBroadcastAsync(string title, string message)
        {
            var users = await _context.Users
                .Where(u => u.Status == UserStatus.Active && u.Role !=UserRole.SuperAdmin)
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var userId in users)
            {
                await _notificationService.SendNotificationAsync(
                    userId,
                    title,
                    message,
                    NotificationType.AdminBrodcast
                );
            }
        }

        
        public async Task SendToUsersAsync(
            List<Guid> userIds,
            string title,
            string message)
        {
            foreach (var userId in userIds.Distinct())
            {
                await _notificationService.SendNotificationAsync(
                    userId,
                    title,
                    message,
                    NotificationType.AdminDirectMessage
                );
            }
        }

        // 3?? Get users (list + search)
        public async Task<List<AdminUserLookupDto>> GetUsersAsync(
            string? search,
            string? category)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    u.FullName.Contains(search) ||
                    u.PhoneNumber.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(category) &&
                Enum.TryParse<UserRole>(category, true, out var role))
            {
                query = query.Where(u => u.Role == role);
            }

            return await query
                .OrderBy(u => u.FullName)
                .Select(u => new AdminUserLookupDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    PhoneNumber=u.PhoneNumber,
                    Status=u.Status
                })
                .ToListAsync();
        }
    }
}
