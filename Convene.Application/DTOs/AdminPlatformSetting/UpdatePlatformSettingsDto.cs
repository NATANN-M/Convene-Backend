using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.AdminPlatformSetting
{
    public class UpdatePlatformSettingsDto
    {
        public decimal CreditPriceETB { get; set; }
        public int InitialOrganizerCredits { get; set; }
        public int EventPublishCost { get; set; }
    }
}
