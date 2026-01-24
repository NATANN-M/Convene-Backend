using Convene.Application.DTOs.Event;
using Convene.Application.DTOs.EventBrowsing;
using Convene.Application.DTOs.PagenationDtos;

namespace Convene.Application.Interfaces
{
    public interface IEventBrowsingService
    {
        Task<PaginatedResult<EventSummaryDto>> GetUpcomingEventsAsync(PagedAndSortedRequest request);
        Task<PaginatedResult<EventSummaryDto>> GetActiveEventsAsync(PagedAndSortedRequest request);
        Task<EventDetailDto> GetEventDetailsAsync(Guid eventId,Guid? userid);
        Task<PaginatedResult<EventSummaryDto>> SearchEventsAsync(EventSearchRequestDto request);
    }
}
