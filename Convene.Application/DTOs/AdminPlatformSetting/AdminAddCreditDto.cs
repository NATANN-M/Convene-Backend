using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.AdminPlatformSetting
{
    public class AdminAddCreditDto
    {
        public Guid UserId { get; set; }   // Organizer UserId
        public int Credits { get; set; }
        public string? Reason { get; set; }
    }
}
