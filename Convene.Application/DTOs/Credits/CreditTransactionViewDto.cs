using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convene.Domain.Enums;

namespace Convene.Application.DTOs.Credits
{
    public class CreditTransactionViewDto
    {

        public Guid UserId { get; set; }
        public string Type { get; set; }
        public int CreditsChanged { get; set; }

        public decimal? TotalAmount { get; set; }
        public string RefernceNumber { get; set; }
        public string CheckoutUrl { get; set; }
        public string Status { get; set; }

        public string? OrganizerName { get; set; }
     

        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

}
