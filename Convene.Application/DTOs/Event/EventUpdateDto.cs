using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Convene.Application.DTOs.Event
{
    public class EventUpdateDto
    {
       
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public string? Venue { get; set; }
        public string? Location { get; set; }

        public DateTime? TicketSalesStart { get; set; }
        public DateTime? TicketSalesEnd { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalCapacity { get; set; }

        // Media for update
        public IFormFile? CoverImage { get; set; } 
        public List<IFormFile>? AdditionalImages { get; set; } = new();
        public List<IFormFile>? Videos { get; set; } = new();
    }
}
