using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Convene.Infrastructure.Hubs;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Domain.Enums;
using Convene.Infrastructure.Persistence;
using Convene.Application.DTOs.Notifications;

namespace Convene.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(IServiceScopeFactory scopeFactory, IHubContext<NotificationHub> hubContext)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
        }

        public async Task SendNotificationAsync(Guid userId, string title, string message, NotificationType type, string? referenceKey = null)
        {
            // Create a new scope with its own DbContext instance
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ConveneDbContext>();

            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                ReferenceKey = referenceKey,
                CreatedAt = DateTime.UtcNow
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            // Send real-time notification
            await _hubContext.Clients.User(userId.ToString())
                .SendAsync("ReceiveNotification", new
                {
                    Title = title,
                    Message = message,
                    Type = type.ToString(),
                    CreatedAt = notification.CreatedAt
                });
        }


        public async Task SendNotificationWithReferenceAsync(
    Guid userId,
    string title,
    string message,
    NotificationType type,
    string referenceKey)
        {
            // Create a new scope with its own DbContext instance
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ConveneDbContext>();

            var alreadySent = await context.Notifications.AnyAsync(n =>
                n.UserId == userId &&
                n.ReferenceKey == referenceKey);

            if (alreadySent)
                return;

            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                ReferenceKey = referenceKey,
                CreatedAt = DateTime.UtcNow
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            await _hubContext.Clients.User(userId.ToString())
                .SendAsync("ReceiveNotification", new
                {
                    Title = title,
                    Message = message,
                    Type = type.ToString(),
                    CreatedAt = notification.CreatedAt
                });
        }


        public async Task<List<UserNotificationDto>> GetUserNotificationsAsync(Guid userId)
        {
            // Create a new scope with its own DbContext instance
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ConveneDbContext>();

            return await context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new UserNotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Title = n.Title,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    Type = n.Type.ToString(),
                    ReferenceKey = n.ReferenceKey
                })
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            // Create a new scope with its own DbContext instance
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ConveneDbContext>();

            var notif = await context.Notifications.FindAsync(notificationId);
            if (notif != null)
            {
                notif.IsRead = true;
                await context.SaveChangesAsync();
            }
        }
    }
}
