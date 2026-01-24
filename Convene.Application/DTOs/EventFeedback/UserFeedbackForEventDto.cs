using System;

namespace Convene.Application.DTOs.Feedback
{
    public class UserFeedbackForEventDto
    {
        public Guid EventId { get; set; }
        public string EventTitle { get; set; } = null!;
        public DateTime EventEndDate { get; set; }

        // Null if user hasn't submitted feedback for this event
        public FeedbackViewDto? Feedback { get; set; }
    }
}
