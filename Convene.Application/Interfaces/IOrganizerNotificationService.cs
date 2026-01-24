using Convene.Application.DTOs.OrganizerNotifications;

namespace Convene.Application.Interfaces
{
    public interface IOrganizerNotificationService
    {
        Task SendToEventAttendeesAsync(
            Guid organizerId,
            Guid eventId,
            string title,
            string message);

        Task<List<OrganizerEventLookupDto>> GetOrganizerEventsAsync(Guid organizerId);
    }
}
