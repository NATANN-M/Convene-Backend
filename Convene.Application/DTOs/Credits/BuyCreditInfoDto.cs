using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.Credits
{
    public class BuyCreditInfoDto
    {
        public decimal CreditPriceETB { get; set; }
        public int EventPublishCost { get; set; }
        public string Message { get; set; }
    }
}
