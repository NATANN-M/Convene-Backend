using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.Credits
{
    public class CreditPaymentResultDto
    {
        public Guid PaymentId { get; set; }
        public string CheckoutUrl { get; set; } = null!;
        public string PaymentReference { get; set; } = null!;
        public string Status { get; set; } = null!;
    }
}
