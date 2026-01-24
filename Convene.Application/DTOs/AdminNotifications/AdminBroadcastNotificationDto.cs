using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.AdminNotifications
{
    public class AdminBroadcastNotificationDto
    {
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
    }
}

