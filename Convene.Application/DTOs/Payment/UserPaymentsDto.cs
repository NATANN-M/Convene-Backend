using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.Payment
{
    public class UserPaymentsDto
    {
     public  Guid userId { get; set; }
        public Guid paymentId { get; set; }
      public  Guid? BookingId { get; set; }
        public string Fullname { get; set; } = null!;
        public string Email { get; set; }
        public decimal Amount { get; set; }
        public string checkOutUrl { get; set; }
        public string PaymentStatus { get; set; }
        public string paymentReferenceNumber { get; set; }
        public DateTime PaymentDate { get; set; }
        public string BookingStatus { get; set; }
        public string EventTitle { get; set; }
        public DateTime EventDate { get; set; }
    }
}
