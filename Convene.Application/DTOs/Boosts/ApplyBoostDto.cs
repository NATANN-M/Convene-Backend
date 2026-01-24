using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.Boosts
{
    public class ApplyBoostDto
    {
        public Guid EventId { get; set; }
        public Guid BoostLevelId { get; set; }
    }
}
