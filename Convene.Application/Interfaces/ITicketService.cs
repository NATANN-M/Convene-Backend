using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convene.Application.DTOs.Booking;
using Convene.Domain.Entities;
using Convene.Domain.Enums;

namespace Convene.Application.Interfaces
{
    public interface ITicketService
    {


        Task<List<Ticket>> CreateTicketsAsync(Booking booking, List<TicketCreateDto> ticketsDto);
        Task<List<TicketViewDto>> GetTicketsForBookingAsync(Booking booking);
        Task CancelTicketsAsync(Booking booking);
        Task UpdateTicketsStatusAsync(Guid bookingId, TicketStatus status);

    }
}
