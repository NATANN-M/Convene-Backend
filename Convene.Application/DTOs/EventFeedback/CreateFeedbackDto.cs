using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.Feedback
{
    public class CreateFeedbackDto
    {
        public int Rating { get; set; }
        public string? Comment { get; set; } // Optional
    }
}
