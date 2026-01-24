using Convene.Application.DTOs.AdminNotifications;

namespace Convene.Application.Interfaces
{
    public interface IAdminNotificationService
    {
        Task SendBroadcastAsync(string title, string message);
        Task SendToUsersAsync(List<Guid> userIds, string title, string message);
        Task<List<AdminUserLookupDto>> GetUsersAsync(
            string? search,
            string? category
        );
    }
}
