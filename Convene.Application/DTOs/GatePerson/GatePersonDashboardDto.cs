using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.GatePerson
{
    public class GatePersonDashboardDto
    {
        public Guid GatePersonId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";

        public string OrganizerName { get; set; } = "";

        public bool HasAssignments { get; set; }
        public List<GatePersonAssignedEventDto> AssignedEvents { get; set; } = new();
    }

}
