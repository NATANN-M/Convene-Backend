using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.Booking;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Domain.Enums;
using Convene.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Services
{
    public class TicketService : ITicketService
    {
        private readonly ConveneDbContext _context;
        private readonly IPricingService _pricingService;

        public TicketService(ConveneDbContext context, IPricingService pricingService)
        {
            _context = context;
            _pricingService = pricingService;
        }

        public async Task<List<Ticket>> CreateTicketsAsync(Booking booking, List<TicketCreateDto> ticketDtos)
        {
            if (ticketDtos == null || !ticketDtos.Any())
                throw new ArgumentException("No ticket data provided.");

            var eventEntity = await _context.Events
                .Include(e => e.TicketTypes)
                    .ThenInclude(t => t.PricingRules)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == booking.EventId);

            if (eventEntity == null)
                throw new KeyNotFoundException("Event not found.");

            var ticketTypeIds = ticketDtos.Select(t => t.TicketTypeId).Distinct().ToList();

            // Fetch prices sequentially to avoid DbContext threading issues
            var pricingTasks = new Dictionary<Guid, decimal>();
            foreach (var id in ticketTypeIds)
            {
                pricingTasks[id] = await _pricingService.GetCurrentPriceAsync(id);
            }

            // Fill single ticket holder info
            if (ticketDtos.Count == 1)
            {
                var single = ticketDtos.First();
                if (string.IsNullOrEmpty(single.HolderName))
                {
                    var user = await _context.Users.AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == booking.UserId);
                    if (user != null)
                    {
                        single.HolderName = user.FullName;
                        single.HolderPhone = user.PhoneNumber ?? string.Empty;
                    }
                }
            }

            var tickets = new List<Ticket>();

            // CHANGED: Update ALL ticket types first before creating any tickets
            foreach (var dto in ticketDtos)
            {
                if (dto.Quantity <= 0)
                    throw new InvalidOperationException("Ticket quantity must be at least 1.");

                var ticketType = eventEntity.TicketTypes.FirstOrDefault(t => t.Id == dto.TicketTypeId);
                if (ticketType == null)
                    throw new KeyNotFoundException($"Ticket type not found for ID: {dto.TicketTypeId}");

                // Atomic update with concurrency check
                int updatedRows = await _context.TicketTypes
                    .Where(t => t.Id == dto.TicketTypeId && t.Sold + dto.Quantity <= t.Quantity)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.Sold, t => t.Sold + dto.Quantity));

                if (updatedRows == 0)
                    throw new InvalidOperationException($"Not enough tickets available for '{ticketType.Name}'. Please try again.");
            }

            // CHANGED: Now create all tickets after successful capacity updates
            foreach (var dto in ticketDtos)
            {
                var ticketType = eventEntity.TicketTypes.FirstOrDefault(t => t.Id == dto.TicketTypeId);

                // Fetch current price 
                decimal price = pricingTasks[ticketType.Id];

                // Optional: enforce holder phone for multiple tickets
                //if (dto.Quantity > 1 && string.IsNullOrEmpty(dto.HolderPhone))
                //    throw new InvalidOperationException("Please provide holder phone when booking multiple tickets.");

                for (int i = 0; i < dto.Quantity; i++)
                {
                    var qrGuid = Guid.NewGuid();

                    // first 4 letters in uppercase
                    string eventCode = eventEntity.Title
                        .Replace(" ", "")
                        .Substring(0, Math.Min(4, eventEntity.Title.Replace(" ", "").Length))
                        .ToUpper();

                    tickets.Add(new Ticket
                    {
                        Id = Guid.NewGuid(),
                        BookingId = booking.Id,
                        TicketTypeId = dto.TicketTypeId,
                        EventId = booking.EventId,
                        HolderName = dto.HolderName ?? "Self",
                        HolderPhone = dto.HolderPhone ?? string.Empty,
                        Price = price,
                        Status = TicketStatus.Reserved,
                        QrCode =$"{eventCode}-{qrGuid:N}"
                    });
                }
            }

            _context.Tickets.AddRange(tickets);
            await _context.SaveChangesAsync();

            return tickets;
        }


        public async Task<List<TicketViewDto>> GetTicketsForBookingAsync(Booking booking)
        {
            if (booking == null)
                throw new ArgumentNullException(nameof(booking));

            var eventEntity = await _context.Events
                .Include(e => e.TicketTypes)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == booking.EventId);

            if (eventEntity == null)
                throw new KeyNotFoundException("Event not found.");

            var ticketTypeIds = booking.Tickets.Select(t => t.TicketTypeId).Distinct().ToList();
            // Fetch prices sequentially
            var pricingTasks = new Dictionary<Guid, decimal>();
            foreach (var id in ticketTypeIds)
            {
                pricingTasks[id] = await _pricingService.GetCurrentPriceAsync(id);
            }

            return booking.Tickets.Select(t => new TicketViewDto
            {
                TicketId = t.Id,
                TicketTypeName = eventEntity.TicketTypes.First(x => x.Id == t.TicketTypeId).Name,
                HolderName = t.HolderName,
                HolderPhone = t.HolderPhone,
                QrCode = t.QrCode,
                Price = pricingTasks[t.TicketTypeId],
                Status = t.Status.ToString()
            }).ToList();
        }

        public async Task CancelTicketsAsync(Booking booking)
        {
            if (booking == null)
                throw new ArgumentNullException(nameof(booking));

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var ticket in booking.Tickets)
                {
                    await _context.TicketTypes
                        .Where(t => t.Id == ticket.TicketTypeId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(t => t.Sold, t => t.Sold > 0 ? t.Sold - 1 : 0));

                    ticket.Status = TicketStatus.Cancelled;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateTicketsStatusAsync(Guid bookingId, TicketStatus newStatus)
        {
            var tickets = await _context.Tickets
                .Where(t => t.BookingId == bookingId)
                .ToListAsync();

            if (!tickets.Any())
                throw new KeyNotFoundException("No tickets found for this booking.");

            foreach (var ticket in tickets)
                ticket.Status = newStatus;

            await _context.SaveChangesAsync();
        }
    }
}
