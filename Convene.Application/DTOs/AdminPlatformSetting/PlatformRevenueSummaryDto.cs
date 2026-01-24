using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.AdminPlatformSetting
{
    public class PlatformRevenueSummaryDto
    {
        // Totals (existing)
        public decimal TotalCreditRevenueETB { get; set; }
        public int TotalCreditsPurchased { get; set; }

        public decimal TotalPublishRevenueETB { get; set; }
        public decimal TotalBoostRevenueETB { get; set; }

        public int TotalEventsPublished { get; set; }
        public int TotalEventsBoosted { get; set; }

        // Charts (new)
        public List<RevenueByWeekDto> WeeklyCreditRevenue { get; set; } = new();
        public List<RevenueByMonthDto> MonthlyCreditRevenue { get; set; } = new();

        public List<RevenueByWeekDto> WeeklyBoostRevenue { get; set; } = new();
        public List<RevenueByMonthDto> MonthlyBoostRevenue { get; set; } = new();
    }

    public class RevenueByWeekDto
    {
        public DateTime Date { get; set; }   // day-based (chart friendly)
        public decimal RevenueETB { get; set; }
    }


    public class RevenueByMonthDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal RevenueETB { get; set; }
    }

}
