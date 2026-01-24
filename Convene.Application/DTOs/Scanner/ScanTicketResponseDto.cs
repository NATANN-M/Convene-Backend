using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.Scanner
{
    public class ScanTicketResponseDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = null!;

        public string EventName { get; set; }
        public DateTime EventStart { get; set; }
        public DateTime EventEnd { get; set; }

        public string TicketHolder { get; set; }
        public string TicketType { get; set; }
    }

}
