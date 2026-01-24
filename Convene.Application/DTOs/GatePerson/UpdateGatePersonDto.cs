using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.GatePerson
{
    public class UpdateGatePersonDto
    {
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? AssignmentsJson { get; set; }

        public bool IsActive { get; set; }
    }

}
