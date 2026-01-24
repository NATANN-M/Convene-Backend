using System.Collections.Generic;

namespace Convene.Application.DTOs.Event
{
    public class EventMediaDto
    {
        public string? CoverImage { get; set; }
        public List<string>? AdditionalImages { get; set; }
        public List<string>? Videos { get; set; }
    }
}
