using Convene.Domain.Common;
using Convene.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Convene.Domain.Entities
{
    public class Event : BaseEntity
    {
        public Guid OrganizerId { get; set; } // FK to OrganizerProfile (User)
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public string? Venue { get; set; }        // e.g., "Millennium Hall"
        public string? Location { get; set; }     // address or coordinates
           //Sales period
        public DateTime TicketSalesStart { get; set; }
        public DateTime TicketSalesEnd { get; set; }
        //Event schedule
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
       
        public string? CoverImageUrl { get; set; } //json string containing images and video 
        [Range(1,int.MaxValue)]
        public int TotalCapacity { get; set; } 
        public EventStatus Status { get; set; } = EventStatus.Draft;

        public int TelegramPostCount { get; set; } = 0;  //telegram post for one event count max 2 

        // Navigation properties
        public EventCategory? Category { get; set; }

       //public OrganizerProfile? Organizer { get; set; }
        public ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public List<EventFeedback> Feedbacks { get; set; } = new();

        public ICollection<UserRecommendation> Recommendations { get; set; } = new List<UserRecommendation>();

        public ICollection<EventBoost> EventBoosts { get; set; } = new List<EventBoost>();


    }
}
