using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.Feedback
{
    public class EventFeedbackSummaryDto
    {
        public Guid EventId { get; set; }
        public double AverageRating { get; set; }
        public int TotalFeedbacks { get; set; }
        public List<FeedbackViewDto> Feedbacks { get; set; } = new();
       

    }
}

