using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Convene.Application.DTOs.Booking;
using Convene.Application.DTOs.Notifications;
using Convene.Application.Helpers.EmailTemplates;
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
    public class BookingService : IBookingService
    {
        private readonly ConveneDbContext _context;
        private readonly IPricingService _pricingService;
        private readonly ITicketService _ticketService;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;
        private readonly ITrackingService _trackingService;
        private readonly ILogger<BookingService> _logger;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue ;
        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public BookingService(ConveneDbContext context, 
            IPricingService pricingService, 
            ITicketService ticketService, 
            IEmailService emailService, 
            INotificationService notificationService,
            ITrackingService trackingSevice,
            ILogger<BookingService> logger,
            IBackgroundTaskQueue backgroundTaskQueue,
            IConfiguration config,IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _config = config;
            _pricingService = pricingService;
            _ticketService = ticketService;
            _emailService = emailService;
            _notificationService = notificationService;
            _trackingService = trackingSevice;
            _logger = logger;
            _backgroundTaskQueue=backgroundTaskQueue;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<BookingResultDto> CreateBookingAsync(BookingCreateDto dto, Guid userId)
        {
            const int maxRetries = 3;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await TryCreateBookingOnceAsync(dto, userId);
                }
                catch (InvalidOperationException ex) when (
                    ex.Message.Contains("Not enough tickets") &&
                    attempt < maxRetries)
                {
                    // Wait before retrying (exponential backoff)
                    await Task.Delay(100 * attempt);
                    continue;
                }
            }

            throw new InvalidOperationException("System is busy due to high demand. Please try again in a moment.");
        }

        private async Task<BookingResultDto> TryCreateBookingOnceAsync(BookingCreateDto dto, Guid userId)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.Tickets == null || !dto.Tickets.Any())
                throw new ArgumentException("At least one ticket must be included in the booking.");

            // First, check all preconditions without transaction
            var eventEntity = await _context.Events
                .Include(e => e.TicketTypes)
                    .ThenInclude(t => t.PricingRules)
                .FirstOrDefaultAsync(e => e.Id == dto.EventId);

            if (eventEntity == null)
                throw new KeyNotFoundException("Event not found.");

            if (eventEntity.Status != EventStatus.Published)
                throw new InvalidOperationException("Event is not available for booking.");

            if (eventEntity.EndDate < DateTime.UtcNow)
                throw new InvalidOperationException("Cannot book an event that has already ended.");

            // Check total capacity
            int ticketsAlreadyBooked = await _context.Tickets
                .Where(t => t.EventId == dto.EventId && t.Status != TicketStatus.Cancelled)
                .CountAsync();

            int requestedTickets = dto.Tickets.Sum(t => t.Quantity);
            if (ticketsAlreadyBooked + requestedTickets > eventEntity.TotalCapacity)
                throw new InvalidOperationException("Total Capcity have Been Reached You Can not Get Ticket.");

            // Check for unpaid previous booking
            bool hasUnpaidBooking = await _context.Bookings
                .Include(b => b.Payments)
                .Where(b => b.EventId == dto.EventId && b.UserId == userId)
                .AnyAsync(b => b.Status == BookingStatus.Pending &&
                               (b.Payments.All(p => p.Status != PaymentStatus.Paid) || !b.Payments.Any()));

            if (hasUnpaidBooking)
                throw new InvalidOperationException("You already have an unpaid booking for this event. Please complete payment first.");

            // Use Serializable isolation for booking creation
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {
                var booking = new Booking
                {
                    Id = Guid.NewGuid(),
                    EventId = dto.EventId,
                    UserId = userId,
                    BookingDate = DateTime.UtcNow,
                    Status = BookingStatus.Pending,
                    TotalAmount = 0
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // Call TicketService - ensure it doesn't use the same _context instance
                var tickets = await _ticketService.CreateTicketsAsync(booking, dto.Tickets);

                booking.TotalAmount = tickets.Sum(t => t.Price);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // AUTO CONFIRM FREE BOOKINGS 
                if (booking.TotalAmount == 0)
                {
                    booking.Status = BookingStatus.Confirmed;

                    var ticketsToActivate = await _context.Tickets
                        .Where(t => t.BookingId == booking.Id)
                        .ToListAsync();

                    foreach (var t in ticketsToActivate)
                        t.Status = TicketStatus.Reserved;

                    _context.Payments.Add(new Payment
                    {
                        Id = Guid.NewGuid(),
                        BookingId = booking.Id,
                        Amount = 0,
                        Status = PaymentStatus.Paid,
                        
                        PaidAt = DateTime.UtcNow,
                        PaymentReference = "FREE-" + Guid.NewGuid().ToString("N")[..8]
                    });

                    await _context.SaveChangesAsync();
                }



                // Extract data needed for background task BEFORE the method returns
                var bookingId = booking.Id;
                var eventId = dto.EventId;

                //  Queue background job AFTER successful commit
                _backgroundTaskQueue.QueueBackgroundWorkItem(async (sp,ct)=>
                {

                    

                    try
                    {
                        _logger.LogInformation("Tracking booking {BookingId} for user {UserId} for event {EventId}",
                            bookingId, userId, eventId);

                        // Create a new scope for the background task
                        using var scope = _serviceScopeFactory.CreateScope();
                        var trackingService = scope.ServiceProvider.GetRequiredService<ITrackingService>();
                        await trackingService.TrackInteractionAsync(userId, eventId, "Book", null);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error tracking booking interaction for booking {BookingId}", bookingId);
                    }
                });

                return new BookingResultDto
                {
                    BookingId = booking.Id,
                    BookingDate = booking.BookingDate,
                    IsFree=booking.TotalAmount==0,
                    TotalAmount = booking.TotalAmount,
                    Tickets = tickets.Select(t => new TicketViewDto
                    {
                        TicketId = t.Id,
                        TicketTypeName = eventEntity.TicketTypes.First(x => x.Id == t.TicketTypeId).Name,
                        HolderName = t.HolderName,
                        HolderPhone = t.HolderPhone,
                        Price = t.Price,
                        Status = t.Status.ToString(),
                        QrCode = t.QrCode
                    }).ToList()
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<MyBookingDto>> GetMyBookingsAsync(Guid userId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Payments)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .AsNoTracking()
                .ToListAsync();

            return bookings.Select(b =>
            {
                var latestPayment = b.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
                var paymentStatus = latestPayment?.Status.ToString() ?? "Pending";

                return new MyBookingDto
                {
                    BookingId = b.Id,
                    EventId = b.EventId,
                    EventTitle = b.Event.Title,
                    BookingDate = b.BookingDate,
                    EventEndingDate=b.Event.EndDate,
                    
                    TotalAmount = b.TotalAmount,
                    BookingStatus = b.Status.ToString(),
                    PaymentStatus = paymentStatus,
                    eventEnded = b.Event.EndDate < DateTime.UtcNow,
                    CheckoutUrl = paymentStatus == "Pending" ? latestPayment?.ChapaCheckoutUrl : null,
                    CanPayNow = paymentStatus == "Pending"
                };
            }).ToList();
        }

        public async Task<List<TicketViewDto>> GetTicketsForBookingAsync(Guid bookingId, Guid userId)
        {
            // Load booking with tickets, payment, and user
            var booking = await _context.Bookings
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.TicketType)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
                throw new KeyNotFoundException("Booking not found.");

            if (booking.Status == BookingStatus.Cancelled)
                throw new InvalidOperationException("Cannot get tickets for a cancelled booking.");

            // Check payment status
            var payment = booking.Payments
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();

            if (payment == null || payment.Status != PaymentStatus.Paid)
                throw new InvalidOperationException("Tickets are available only for paid bookings.");

            // Map tickets to DTO
            var ticketsDto = booking.Tickets.Select(t => new TicketViewDto
            {
                TicketId = t.Id,
                TicketTypeName = t.TicketType.Name,
                HolderName = t.HolderName,
                HolderPhone = t.HolderPhone,
                Price = t.Price,
                Status = t.Status.ToString(),
                QrCode = t.QrCode
            }).ToList();

            return ticketsDto;
        }



        public async Task<List<BookingSummaryDto>> GetUserBookingsAsync(Guid userId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Tickets)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .AsNoTracking()
                .ToListAsync();

            var result = new List<BookingSummaryDto>(bookings.Count);

            foreach (var booking in bookings)
            {
                var ticketsDto = await _ticketService.GetTicketsForBookingAsync(booking);
                var total = ticketsDto.Sum(t => t.Price);

                result.Add(new BookingSummaryDto
                {
                    BookingId = booking.Id,
                    EventTitle = booking.Event.Title,
                    BookingDate = booking.BookingDate,
                    TotalAmount = total,
                    Status = booking.Status.ToString(),
                    IsFreeEvent = total == 0,
                    Tickets = ticketsDto
                });
            }

            return result;
        }

        public async Task<BookingSummaryDto?> GetBookingByIdAsync(Guid bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Tickets)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                return null;

            var ticketsDto = await _ticketService.GetTicketsForBookingAsync(booking);
            var total = ticketsDto.Sum(t => t.Price);

            return new BookingSummaryDto
            {
                BookingId = booking.Id,
                EventTitle = booking.Event.Title,
                BookingDate = booking.BookingDate,
                TotalAmount = total,
                Status = booking.Status.ToString(),
                IsFreeEvent = total == 0,
                Tickets = ticketsDto
            };
        }

        public async Task<bool> CancelBookingAsync(Guid bookingId, Guid userId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Tickets)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
                throw new KeyNotFoundException("Booking not found.");

            if (booking.Status == BookingStatus.Confirmed)
                throw new InvalidOperationException("Confirmed bookings cannot be cancelled.");

            booking.Status = BookingStatus.Cancelled;
            await _ticketService.CancelTicketsAsync(booking);

            await _context.SaveChangesAsync();
            return true;
        }


        public async Task SendBookingConfirmationEmailAsync(BookingEmailDto dto)
        {
            if (dto == null)
                return;

            string bookingLink = $"{_config["BookingConfirmationLink"]}{dto.BookingId}";

            string htmlBody = BookingTemplateHelper.GetBookingConfirmationHtml(
                userName: dto.UserFullName,
                eventName: dto.EventTitle,
                eventDate: dto.EventStartDate,
                eventLocation: dto.EventLocation,
                totalAmount: dto.TotalAmount,
                bookingLink: bookingLink
            );

            string subject = $"Booking Confirmed: {dto.EventTitle}";

            await _emailService.SendEmailAsync(dto.UserEmail, subject, htmlBody);
        }



    }
}
