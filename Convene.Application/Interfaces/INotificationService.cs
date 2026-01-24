using Convene.Application.DTOs.Notifications;
using Convene.Domain.Entities;
using Convene.Domain.Enums;

namespace Convene.Application.Interfaces
{
    public interface INotificationService
    {
       public  Task SendNotificationWithReferenceAsync(
        Guid userId,
        string title,
        string message,
        NotificationType type,
        string referenceKey);
       

        Task SendNotificationAsync(Guid userId, string title, string message, NotificationType type, string? referenceKey = null);

        Task<List<UserNotificationDto>> GetUserNotificationsAsync(Guid userId);
        Task MarkAsReadAsync(Guid notificationId);
    }
}
