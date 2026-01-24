using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Convene.Application.DTOs.Payment;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Domain.Enums;
using Convene.Infrastructure.Persistence;
using Convene.Infrastructure.Services;
using Xunit;

namespace Convene.UnitTests.Infrastructure.Services
{
    public class PaymentServiceTests
    {
        private readonly ConveneDbContext _context;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ITicketService> _ticketServiceMock;
        private readonly Mock<IBookingService> _bookingServiceMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<IBackgroundTaskQueue> _backgroundTaskQueueMock;
        private readonly Mock<ICreditService> _creditServiceMock;
        private readonly PaymentService _paymentService;

        public PaymentServiceTests()
        {
            var options = new DbContextOptionsBuilder<ConveneDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new ConveneDbContext(options);

            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://api.chapa.co")
            };

            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(x => x["Chapa:SecretKey"]).Returns("test-secret-key");
            _configurationMock.Setup(x => x["Chapa:BaseUrl"]).Returns("https://api.chapa.co/v1/");
            _configurationMock.Setup(x => x["Chapa:BookingCallbackUrl"]).Returns("https://test.com/callback");
            _configurationMock.Setup(x => x["Chapa:CreditCallbackUrl"]).Returns("https://test.com/credit/callback");

            _ticketServiceMock = new Mock<ITicketService>();
            _bookingServiceMock = new Mock<IBookingService>();
            _notificationServiceMock = new Mock<INotificationService>();
            _backgroundTaskQueueMock = new Mock<IBackgroundTaskQueue>();
            _creditServiceMock = new Mock<ICreditService>();

            _paymentService = new PaymentService(
                _context,
                _httpClientFactoryMock.Object,
                _configurationMock.Object,
                _ticketServiceMock.Object,
                _bookingServiceMock.Object,
                _notificationServiceMock.Object,
                _backgroundTaskQueueMock.Object,
                _creditServiceMock.Object
            );
        }

        [Fact]
        public async Task InitializePayment_BookingNotPending_ThrowsInvalidOperationException()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var booking = new Booking
            {
                Id = bookingId,
                Status = BookingStatus.Confirmed, // Not Pending
                User = new User 
                { 
                    Email = "test@example.com", 
                    FullName = "Test User",
                    PasswordHash = "hashed_pw",
                    PhoneNumber = "1234567890" 
                }
            };
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            var request = new InitializePaymentRequest { BookingId = bookingId };

            // Act
            Func<Task> act = async () => await _paymentService.InitializePaymentAsync(request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Booking is not pending payment.");
        }

        [Fact]
        public async Task VerifyPayment_ChapaSuccess_UpdatesStatusToPaid()
        {
            // Arrange
            var txRef = "tx-123";
            var bookingId = Guid.NewGuid();
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                PaymentReference = txRef,
                Status = PaymentStatus.Pending,
                PayerEmail = "test@example.com",
                PayerName = "Test User",
                PayerPhone = "1234567890",
                BookingId = bookingId,
                Booking = new Booking
                {
                    Id = bookingId,
                    Status = BookingStatus.Pending,
                    User = new User 
                    { 
                        Email = "test@example.com", 
                        FullName = "Test User", 
                        PasswordHash = "hash", 
                        PhoneNumber = "123" 
                    },
                    Event = new Event { Title = "Test Event" }
                }
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Mock Chapa Verify Response
            var successResponse = "{\"status\":\"success\",\"data\":{\"status\":\"success\"}}";
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(successResponse)
                });

            // Act
            var result = await _paymentService.VerifyPaymentAsync(txRef);

            // Assert
            result.Should().BeTrue();
            
            var updatedPayment = await _context.Payments.FirstAsync(p => p.PaymentReference == txRef);
            updatedPayment.Status.Should().Be(PaymentStatus.Paid);
            updatedPayment.PaidAt.Should().NotBeNull();

            var updatedBooking = await _context.Bookings.FirstAsync(b => b.Id == bookingId);
            updatedBooking.Status.Should().Be(BookingStatus.Confirmed);

            // Verify ticket status update called
            _ticketServiceMock.Verify(x => x.UpdateTicketsStatusAsync(bookingId, TicketStatus.Reserved), Times.Once);
        }

        [Fact]
        public async Task VerifyPayment_ChapaFailure_UpdatesStatusToFailed()
        {
            // Arrange
            var txRef = "tx-fail-123";
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                PaymentReference = txRef,
                Status = PaymentStatus.Pending,
                PayerEmail = "payer@test.com",
                PayerName = "Payer Name",
                PayerPhone = "0000000000",
                Booking = new Booking 
                { 
                    User = new User 
                    { 
                        Email = "payer@test.com",
                        FullName = "User", 
                        PasswordHash = "hash", 
                        PhoneNumber = "123" 
                    }, 
                    Event = new Event { Title = "Test Event" } 
                }
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Mock Chapa Fail Response
            var failResponse = "{\"status\":\"failed\",\"data\":null}";
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(failResponse)
                });

            // Act
            var result = await _paymentService.VerifyPaymentAsync(txRef);

            // Assert
            result.Should().BeFalse();
            
            var updatedPayment = await _context.Payments.FirstAsync(p => p.PaymentReference == txRef);
            updatedPayment.Status.Should().Be(PaymentStatus.Failed);
        }
    }
}
