using System;
using System.Collections.Generic;
using System.Linq;
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

        // ---------------- Add Ticket Type ----------------
        public async Task<TicketTypeResponseDto> AddTicketTypeAsync(Guid eventId, TicketTypeCreateDto dto)
        {
            var ev = await _context.Events
                .Include(e => e.TicketTypes)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null)
                throw new KeyNotFoundException("Event not found");

            var currentTotal = ev.TicketTypes.Sum(t => t.Quantity);

            if (currentTotal + dto.Quantity > ev.TotalCapacity)
            {
                throw new InvalidOperationException(
                    $"Total capacity of {ev.TotalCapacity} would be exceeded. Please adjust event capacity or ticket quantities."
                );
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

        // ---------------- Update Ticket Type ----------------
        public async Task<TicketTypeResponseDto> UpdateTicketTypeAsync(Guid ticketTypeId, TicketTypeCreateDto dto)
        {
            var ticket = await _context.TicketTypes
                .FirstOrDefaultAsync(t => t.Id == ticketTypeId);

            if (ticket == null)
                throw new KeyNotFoundException("Ticket type not found");

            var ev = await _context.Events
                .Include(e => e.TicketTypes)
                .FirstOrDefaultAsync(e => e.Id == ticket.EventId);

            if (ev == null)
                throw new KeyNotFoundException("Event not found");

            // Capacity check (excluding this ticket)
            var totalOtherTickets = ev.TicketTypes
                .Where(t => t.Id != ticketTypeId)
                .Sum(t => t.Quantity);

            if (totalOtherTickets + dto.Quantity > ev.TotalCapacity)
            {
                throw new InvalidOperationException(
                    $"Cannot update ticket. Total capacity of {ev.TotalCapacity} would be exceeded. Please adjust event capacity or ticket quantities."
                );
            }

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

        // ---------------- Activate / Deactivate ----------------
        public async Task<string> SetActiveStatusAsync(Guid ticketTypeId, bool isActive)
        {
            var ticket = await _context.TicketTypes
                .FirstOrDefaultAsync(t => t.Id == ticketTypeId);

            if (ticket == null)
                throw new KeyNotFoundException("Ticket type not found");

            ticket.IsActive = isActive;
            await _context.SaveChangesAsync();

            return isActive
                ? "Ticket type activated successfully."
                : "Ticket type deactivated successfully.";
        }

        // ---------------- Remove Ticket Type ----------------
        public async Task<string> RemoveTicketTypeAsync(Guid ticketTypeId)
        {
            var ticket = await _context.TicketTypes
                .FirstOrDefaultAsync(t => t.Id == ticketTypeId);

            if (ticket == null)
                throw new KeyNotFoundException("Ticket type not found");

            _context.TicketTypes.Remove(ticket);
            await _context.SaveChangesAsync();

            return "Ticket type removed successfully.";
        }
    }
}
