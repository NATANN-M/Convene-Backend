using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.OrganizerNotifications;
using Convene.Application.Interfaces;
using Convene.Domain.Enums;
using Convene.Infrastructure.Persistence;

namespace Convene.Infrastructure.Services
{
    public class OrganizerNotificationService : IOrganizerNotificationService
    {
        private readonly ConveneDbContext _context;
        private readonly INotificationService _notificationService;

        public OrganizerNotificationService(
            ConveneDbContext context,
            INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // 1?? Send notification to booked users
        public async Task SendToEventAttendeesAsync(
            Guid organizerId,
            Guid eventId,
            string title,
            string message)
        {
            // Validate event ownership
            var ev = await _context.Events
                .Where(e => e.Id == eventId && e.OrganizerId == organizerId)
                .FirstOrDefaultAsync();

            if (ev == null)
                throw new Exception("Event not found or not owned by organizer");

            // Get confirmed attendees
            var userIds = await _context.Bookings
                .Where(b =>
                    b.EventId == eventId &&
                    b.Status == BookingStatus.Confirmed)
                .Select(b => b.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var userId in userIds)
            {
                await _notificationService.SendNotificationAsync(
                    userId,
                    $"{ev.Title}: {title}",
                    message,
                    NotificationType.OrganizerMessage
                );
            }
        }

        // 2?? Organizer events (for dropdown)
        public async Task<List<OrganizerEventLookupDto>> GetOrganizerEventsAsync(Guid organizerId)
        {
            return await _context.Events
                .Where(e => e.OrganizerId == organizerId)
                .OrderByDescending(e => e.StartDate)
                .Select(e => new OrganizerEventLookupDto
                {
                    Id = e.Id,
                    Title = e.Title
                })
                .ToListAsync();
        }
    }
}
