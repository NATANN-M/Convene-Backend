using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.GatePerson
{
    public class GatePersonAssignmentDto
    {
        public Guid EventId { get; set; }
        public string EventName { get; set; } = null!;
        public string? GateName { get; set; } = null!;
        public DateTime AssignmentDate { get; set; }
        public string? Notes { get; set; }
    }
}
