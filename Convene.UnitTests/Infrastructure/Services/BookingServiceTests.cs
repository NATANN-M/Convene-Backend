using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Convene.Application.DTOs.Booking;

using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Domain.Enums;
using Convene.Infrastructure.Persistence;
using Convene.Infrastructure.Services;
using Xunit;

namespace Convene.UnitTests.Infrastructure.Services
{
    public class BookingServiceTests
    {
        private readonly ConveneDbContext _context;
        private readonly Mock<IPricingService> _pricingServiceMock;
        private readonly Mock<ITicketService> _ticketServiceMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<ITrackingService> _trackingServiceMock;
        private readonly Mock<ILogger<BookingService>> _loggerMock;
        private readonly Mock<IBackgroundTaskQueue> _backgroundTaskQueueMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
        private readonly BookingService _bookingService;

        public BookingServiceTests()
        {
            var options = new DbContextOptionsBuilder<ConveneDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new ConveneDbContext(options);

            _pricingServiceMock = new Mock<IPricingService>();
            _ticketServiceMock = new Mock<ITicketService>();
            _emailServiceMock = new Mock<IEmailService>();
            _notificationServiceMock = new Mock<INotificationService>();
            _trackingServiceMock = new Mock<ITrackingService>();
            _loggerMock = new Mock<ILogger<BookingService>>();
            _backgroundTaskQueueMock = new Mock<IBackgroundTaskQueue>();
            _configMock = new Mock<IConfiguration>();
            _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();

            _bookingService = new BookingService(
                _context,
                _pricingServiceMock.Object,
                _ticketServiceMock.Object,
                _emailServiceMock.Object,
                _notificationServiceMock.Object,
                _trackingServiceMock.Object,
                _loggerMock.Object,
                _backgroundTaskQueueMock.Object,
                _configMock.Object,
                _serviceScopeFactoryMock.Object
            );
        }

        [Fact]
        public async Task CreateBooking_EventNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var dto = new BookingCreateDto { EventId = Guid.NewGuid(), Tickets = new List<TicketCreateDto> { new TicketCreateDto { Quantity = 1 } } };
            var userId = Guid.NewGuid();

            // Act
            Func<Task> act = async () => await _bookingService.CreateBookingAsync(dto, userId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Event not found.");
        }

        [Fact]
        public async Task CreateBooking_EventNotPublished_ThrowsInvalidOperationException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Title = "Test Event",
                Id = eventId,
                Status = EventStatus.Draft, // Not Published
                EndDate = DateTime.UtcNow.AddDays(1)
            };
            _context.Events.Add(eventEntity);
            await _context.SaveChangesAsync();

            var dto = new BookingCreateDto { EventId = eventId, Tickets = new List<TicketCreateDto> { new TicketCreateDto { Quantity = 1 } } };

            // Act
            Func<Task> act = async () => await _bookingService.CreateBookingAsync(dto, Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Event is not available for booking.");
        }

        [Fact]
        public async Task CreateBooking_EventEnded_ThrowsInvalidOperationException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Title = "Test Event",
                Id = eventId,
                Status = EventStatus.Published,
                EndDate = DateTime.UtcNow.AddDays(-1) // Ended
            };
            _context.Events.Add(eventEntity);
            await _context.SaveChangesAsync();

            var dto = new BookingCreateDto { EventId = eventId, Tickets = new List<TicketCreateDto> { new TicketCreateDto { Quantity = 1 } } };

            // Act
            Func<Task> act = async () => await _bookingService.CreateBookingAsync(dto, Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Cannot book an event that has already ended.");
        }

        [Fact]
        public async Task CreateBooking_NotEnoughCapacity_ThrowsInvalidOperationException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Title = "Test Event",
                Id = eventId,
                Status = EventStatus.Published,
                EndDate = DateTime.UtcNow.AddDays(1),
                TotalCapacity = 10
            };
            _context.Events.Add(eventEntity);

            // Mock 10 existing tickets
            _context.Tickets.AddRange(Enumerable.Range(0, 10).Select(_ => new Ticket 
            { 
                EventId = eventId, 
                Status = TicketStatus.Reserved,
                TicketTypeId = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                HolderName = "Holder",
                HolderPhone = "123",
                QrCode = "QR"
            }));
            await _context.SaveChangesAsync();

            var dto = new BookingCreateDto { EventId = eventId, Tickets = new List<TicketCreateDto> { new TicketCreateDto { Quantity = 1 } } };

            // Act
            Func<Task> act = async () => await _bookingService.CreateBookingAsync(dto, Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Total Capcity have Been Reached You Can not Get Ticket.");
        }

        [Fact]
        public async Task CreateBooking_ValidRequest_CreatesBookingAndCallsTicketService()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var ticketTypeId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var eventEntity = new Event
            {
                Title = "Test Event",
                Id = eventId,
                Status = EventStatus.Published,
                EndDate = DateTime.UtcNow.AddDays(1),
                TotalCapacity = 100,
                TicketTypes = new List<TicketType>
                {
                    // Minimal required setup for TicketType if accessed by service
                    new TicketType { Id = ticketTypeId, Name = "General", BasePrice = 100 } 
                }
            };
            _context.Events.Add(eventEntity);
            await _context.SaveChangesAsync();

            var dto = new BookingCreateDto
            {
                EventId = eventId,
                Tickets = new List<TicketCreateDto>
                {
                    new TicketCreateDto { TicketTypeId = ticketTypeId, Quantity = 2 }
                }
            };

            // Mock TicketService response
            var mockTickets = new List<Ticket>
            {
                new Ticket { Id = Guid.NewGuid(), Price = 100, TicketTypeId = ticketTypeId },
                new Ticket { Id = Guid.NewGuid(), Price = 100, TicketTypeId = ticketTypeId }
            };
            _ticketServiceMock.Setup(x => x.CreateTicketsAsync(It.IsAny<Booking>(), It.IsAny<List<TicketCreateDto>>()))
                .ReturnsAsync(mockTickets);

            // Act
            var result = await _bookingService.CreateBookingAsync(dto, userId);

            // Assert
            result.Should().NotBeNull();
            result.TotalAmount.Should().Be(200);
            
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == result.BookingId);
            booking.Should().NotBeNull();
            booking.Status.Should().Be(BookingStatus.Pending); // Should be pending until paid
            booking.UserId.Should().Be(userId);

            // Verify TicketService called
            _ticketServiceMock.Verify(x => x.CreateTicketsAsync(It.Is<Booking>(b => b.Id == result.BookingId), dto.Tickets), Times.Once);

            // Verify Background Task Queue called for tracking
            _backgroundTaskQueueMock.Verify(x => x.QueueBackgroundWorkItem(It.IsAny<Func<IServiceProvider, CancellationToken, Task>>()), Times.Once);
        }
    }
}
