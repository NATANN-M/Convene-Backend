using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.Feedback
{
    public class FeedbackViewDto
    {
        public Guid FeedbackId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string UserName { get; set; } = null!;
        public string? UserAvatarUrl { get; set; } // Optional
        public string? EventTitle { get; set; } // Added for context
        public DateTime CreatedAt { get; set; }
    }
}

