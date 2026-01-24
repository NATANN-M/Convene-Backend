using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.Event;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Infrastructure.Persistence;

namespace Convene.Infrastructure.Services
{
    public class TicketTypeService : ITicketTypeService
    {
        private readonly ConveneDbContext _context;

        public TicketTypeService(ConveneDbContext context)
        {
            _context = context;
        }

        public async Task<TicketTypeResponseDto> AddTicketTypeAsync(Guid eventId, TicketTypeCreateDto dto)
        {
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (ev == null) throw new Exception("Event not found");

            if(ev.TotalCapacity < ev.TicketTypes.Sum(t => t.Quantity) + dto.Quantity)
            {
                throw new Exception("Total ticket quantity exceeds event capacity");
            }

            var ticket = new TicketType
            {
                Id = Guid.NewGuid(),
                EventId = ev.Id,
                Name = dto.Name,
                Description = dto.Description,
                BasePrice = dto.BasePrice,
                Quantity = dto.Quantity,
                Sold = 0
            };

            await _context.TicketTypes.AddAsync(ticket);
            await _context.SaveChangesAsync();

            return new TicketTypeResponseDto
            {
                Id = ticket.Id,
                Name = ticket.Name,
                Description = ticket.Description,
                BasePrice = ticket.BasePrice,
                Quantity = ticket.Quantity,
                Sold = ticket.Sold
            };
        }

        public async Task<TicketTypeResponseDto> UpdateTicketTypeAsync(Guid ticketTypeId, TicketTypeCreateDto dto)
        {
            var ticket = await _context.TicketTypes.FirstOrDefaultAsync(t => t.Id == ticketTypeId);
            if (ticket == null) throw new Exception("Ticket type not found");

            ticket.Name = dto.Name;
            ticket.Description = dto.Description;
            ticket.BasePrice = dto.BasePrice;
            ticket.Quantity = dto.Quantity;

            await _context.SaveChangesAsync();

            return new TicketTypeResponseDto
            {
                Id = ticket.Id,
                Name = ticket.Name,
                Description = ticket.Description,
                BasePrice = ticket.BasePrice,
                Quantity = ticket.Quantity,
                Sold = ticket.Sold
            };
        }


        // Activate or Deactivate ticket type
        public async Task<string> SetActiveStatusAsync(Guid ticketTypeId, bool isActive)
        {
            var ticket = await _context.TicketTypes.FirstOrDefaultAsync(t => t.Id == ticketTypeId);
            if (ticket == null) throw new Exception("Ticket type not found");

            ticket.IsActive = isActive;
            await _context.SaveChangesAsync();

            return isActive ? "Ticket type activated successfully." : "Ticket type deactivated successfully.";
        }

        // Remove ticket type
        public async Task<string> RemoveTicketTypeAsync(Guid ticketTypeId)
        {
            var ticket = await _context.TicketTypes.FirstOrDefaultAsync(t => t.Id == ticketTypeId);
            if (ticket == null) throw new Exception("Ticket type not found");

            _context.TicketTypes.Remove(ticket);
            await _context.SaveChangesAsync();

            return "Ticket type removed successfully.";
        }
    }

}
