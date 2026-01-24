using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.OrganizerProfile
{
    public class UpdateOrganizerProfileDto
    {
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }

        public string? BusinessName { get; set; }
        public string? BusinessEmail { get; set; }
    }

}
