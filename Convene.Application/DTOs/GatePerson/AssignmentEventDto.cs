using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.GatePerson
{
    public class AssignmentEventDto
    {
        public Guid EventId { get; set; }
        public string EventName { get; set; } = "";
    }
}
