using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convene.Domain.Enums;

namespace Convene.Application.DTOs.Credits
{
    public class CreditPurchaseResponseDto
    {
        public Guid CreditTransactionId { get; set; }
       // public string PaymentReference { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public int CreditsPurchased { get; set; }
        public string Descritption { get; set; }
    }

}
