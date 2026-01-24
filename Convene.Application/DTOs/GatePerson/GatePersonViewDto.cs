using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.GatePerson
{
    public class GatePersonViewDto
    {
       // public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
      //  public string? ProfileImageUrl { get; set; }
       // public Guid CreatedByOrganizerId { get; set; }
        public List<GatePersonAssignmentDto> Assignments { get; set; } = new List<GatePersonAssignmentDto>();
        public bool IsActive { get; set; }
    }

}
