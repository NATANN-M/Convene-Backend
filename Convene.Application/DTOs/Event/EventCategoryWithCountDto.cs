using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.Event
{
    public class EventCategoryWithCountDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int EventCount { get; set; } 
    }

}
