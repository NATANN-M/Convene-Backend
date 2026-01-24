using Convene.Application.DTOs.OrganizerAnalytics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Convene.Application.Interfaces
{
    public interface IOrganizerAnalyticsService
    {
        Task<OrganizerOverviewAnalyticsDto> GetOverviewAnalyticsAsync(Guid organizerId);
        Task<List<EventAnalyticsListItemDto>> GetOrganizerAnalyticsEventsAsync(Guid organizerId);
        Task<EventAnalyticsDto> GetEventAnalyticsAsync(Guid organizerId, Guid eventId);

        Task<List<OrganizerBookedUserDto>> GetBookedUsersAsync(
            Guid organizerId,
            Guid eventId,
            DateTime? startDate,
            DateTime? endDate,
            Guid? ticketTypeId);

        Task<ExportFileDto> ExportBookedUsersAsync(
       Guid organizerId,
       Guid eventId,
       DateTime? startDate,
       DateTime? endDate,
       Guid? ticketTypeId,
       string format);

    }
}
