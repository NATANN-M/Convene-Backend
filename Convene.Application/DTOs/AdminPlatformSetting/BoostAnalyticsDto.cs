using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.AdminPlatformSetting
{
    public class BoostAnalyticsDto
    {
        public Guid BoostLevelId { get; set; }
        public string BoostLevelName { get; set; } = null!;
        public int TotalTimesUsed { get; set; }
        public decimal RevenueGeneratedETB { get; set; }
        public int TotalBoosts { get; set; }
        public int TotalCreditsUsed { get; set; }
        public int TotalEventsBoosted { get; set; }
    }
}
