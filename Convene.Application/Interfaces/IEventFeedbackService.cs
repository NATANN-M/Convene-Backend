using Convene.Application.DTOs.EventFeedback;
using Convene.Application.DTOs.Feedback;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Convene.Application.Interfaces
{
    public interface IEventFeedbackService
    {
        // actions
        Task<FeedbackViewDto> SubmitFeedbackAsync(Guid eventId, CreateFeedbackDto dto, Guid userId);
        Task<FeedbackViewDto> UpdateFeedbackAsync(Guid feedbackId, UpdateFeedbackDto dto, Guid userId);
        Task<bool> DeleteFeedbackAsync(Guid feedbackId, Guid userId);

        // read
        Task<List<FeedbackViewDto>> GetFeedbacksForEventAsync(Guid eventId);
        Task<EventFeedbackSummaryDto> GetEventFeedbackSummaryAsync(Guid eventId);

        // user-specific
        Task<UserFeedbackForEventDto?> GetUserFeedbackForEventAsync(Guid userId, Guid eventId);
        Task<List<FeedbackViewDto>> GetUserFeedbackHistoryAsync(Guid userId);

        // list of events user can give feedback on (eligible)
        Task<List<EligibleEventForFeedbackDto>> GetEligibleEventsForFeedbackAsync(Guid userId);
        //feedbacks given for all events he organized for organizer
        Task<List<FeedbackViewDto>> GetFeedbacksForOrganizerAsync(Guid organizerId);




    }
}
