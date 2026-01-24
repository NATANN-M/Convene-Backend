using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.Boosts
{
    public class BoostResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
    }
}
