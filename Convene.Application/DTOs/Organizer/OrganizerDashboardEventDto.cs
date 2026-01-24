using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.Organizer
{
    public class OrganizerDashboardEventDto
    {
        public Guid EventId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public string Venue { get; set; } = null!;
        public string Location { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalCapacity { get; set; }
        public string Status { get; set; } = null!;
        public string? BannerImageUrl { get; set; }
    }

}
