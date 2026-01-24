using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.EventFeedback
{
    public class EligibleEventForFeedbackDto
    {
        public Guid EventId { get; set; }
        public string Title { get; set; } = null!;
        public DateTime EndDate { get; set; }
        public string OrganizerName { get; set; } = null!;
        public string? CoverImageUrl { get; set; }
    }

}
