using Convene.Application.DTOs.Event;
using System;
using System.Threading.Tasks;

namespace Convene.Application.Interfaces
{
    public interface ITicketTypeService
    {
        Task<TicketTypeResponseDto> AddTicketTypeAsync(Guid eventId, TicketTypeCreateDto dto);
        Task<TicketTypeResponseDto> UpdateTicketTypeAsync(Guid ticketTypeId, TicketTypeCreateDto dto);
        Task<string> SetActiveStatusAsync(Guid ticketTypeId, bool isActive);
        Task<string> RemoveTicketTypeAsync(Guid ticketTypeId);
    }
}
