using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Convene.Application.DTOs.Image_Generation
{
    public class EventCoverDto
    {
        // REQUIRED
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public string Venue { get; set; }

        // OPTIONAL
        public string? Description { get; set; }
        public List<string>? Styles { get; set; }
        public string? PrimaryColor { get; set; }
        public string? ColorTheme { get; set; }
        public string? Brightness { get; set; }

        // OPTIONAL: Upload image to modify
        public IFormFile? ExistingImage { get; set; }

        // OPTIONAL: Refine previous
        public string? RefineFromId { get; set; }
        public string? RefinementNotes { get; set; }
    }
}
