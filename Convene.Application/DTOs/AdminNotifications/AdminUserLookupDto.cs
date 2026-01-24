using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convene.Domain.Enums;

namespace Convene.Application.DTOs.AdminNotifications
{
    public class AdminUserLookupDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = null!;
        public string PhoneNumber { get; set; }
        public UserStatus Status { get; set; }
    }
}
