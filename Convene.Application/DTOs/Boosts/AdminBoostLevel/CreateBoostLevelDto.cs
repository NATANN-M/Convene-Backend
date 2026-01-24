using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.Boosts.AdminBoostLevel
{
    public class CreateBoostLevelDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int CreditCost { get; set; }
        public int DurationHours { get; set; }
        public int Weight { get; set; }
    }
}
