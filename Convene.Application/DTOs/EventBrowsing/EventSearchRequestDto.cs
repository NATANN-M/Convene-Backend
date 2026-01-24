using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convene.Application.DTOs.PagenationDtos;

namespace Convene.Application.DTOs.EventBrowsing
{
    public class EventSearchRequestDto: PagedAndSortedRequest
    {
        public string? OrganizerName { get; set; }
        public string? CategoryName { get; set; }
        public string? Venue { get; set; }

        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }

        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        public string? Keyword { get; set; }  // search on title, desc, venue
    }

}
