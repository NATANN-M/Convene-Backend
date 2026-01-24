using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convene.Domain.Enums;

namespace Convene.Application.DTOs.Credits
{
    public class CreditTransactionFilterDto
    {
        public string? Type { get; set; }          // Purchase | PublishEvent | BoostEvent
        public PaymentStatus? Status { get; set; } // Paid | Pending | Failed

        
        public Guid? OrganizerUserId { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

}
