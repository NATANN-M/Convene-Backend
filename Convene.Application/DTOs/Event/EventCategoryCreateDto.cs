using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.Event
{
    public class EventCategoryCreateDto
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
    }
}
