using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convene.Application.DTOs.Organizer;

namespace Convene.Application.Interfaces
{
    public interface IOrganizerDashboardService
    {
        Task<List<OrganizerDashboardEventDto>> GetDashboardEventsAsync(Guid organizerId);
    }

}
