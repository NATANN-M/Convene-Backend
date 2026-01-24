using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Convene.Application.DTOs.Notifications;
using Convene.Application.DTOs.Payment;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Domain.Enums;
using Convene.Infrastructure.Persistence;
using System;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ConveneDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ITicketService _ticketService;
        private readonly IBookingService _bookingService;
        private readonly INotificationService _notificationService;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly ICreditService _creditService;
        public PaymentService(
            ConveneDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ITicketService ticketService,
            IBookingService bookingService,
            INotificationService notificationService,
            IBackgroundTaskQueue backgroundTaskQueue,
            ICreditService creditService)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
            _ticketService = ticketService;
            _bookingService = bookingService;
            _notificationService = notificationService;
            _backgroundTaskQueue = backgroundTaskQueue;
            _creditService = creditService;
        }

        public async Task<PaymentResultDto> InitializePaymentAsync(InitializePaymentRequest request)
        {
            // Load booking with user data included
            var booking = await _context.Bookings
                .Include(b => b.Payments)
                .Include(b => b.User) // Include user to get their data
                .FirstOrDefaultAsync(b => b.Id == request.BookingId)
                ?? throw new KeyNotFoundException("Booking not found.");

            if (booking.Status != BookingStatus.Pending)
                throw new InvalidOperationException("Booking is not pending payment.");

         

            // Prevent duplicate pending payments or failed payment
            var existingPending = booking.Payments
                .FirstOrDefault(p => p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Paid || p.Status==PaymentStatus.Failed);

            if (existingPending != null)
            {
                return new PaymentResultDto
                {
                    PaymentId = existingPending.Id,
                    CheckoutUrl = existingPending.ChapaCheckoutUrl,
                    PaymentReference = existingPending.PaymentReference,
                    Status = existingPending.Status
                };
            }

            var txRef = Guid.NewGuid().ToString();

            var baseUrl = _configuration["Chapa:BaseUrl"];
            var callbackUrl = _configuration["Chapa:BookingCallbackUrl"];
            
            var chapaRequest = new
            {
                amount = booking.TotalAmount, 
                currency = "ETB",
                email = booking.User.Email,   
                first_name = booking.User.FullName,
                phone_number = SanitizePhoneNumber(booking.User.PhoneNumber), 
                tx_ref = txRef,
                callback_url = callbackUrl
            };

            var apiKey = _configuration["Chapa:SecretKey"];
            
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}transaction/initialize")
            {
                Content = JsonContent.Create(chapaRequest)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var transaction = await _context.Database.BeginTransactionAsync();

            var response = await _httpClient.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Chapa init failed: {content}");

            var chapaResponse = JsonSerializer.Deserialize<ChapaInitResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("Failed to parse Chapa response.");

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                Amount = booking.TotalAmount, // From booking
                Status = PaymentStatus.Pending,
                PaymentReference = txRef,
                ChapaCheckoutUrl = chapaResponse.Data.CheckoutUrl,
                PayerName = booking.User.FullName, // From user
                PayerEmail = booking.User.Email,   // From user
                PayerPhone = booking.User.PhoneNumber, // From user
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new PaymentResultDto
            {
                PaymentId = payment.Id,
                CheckoutUrl = payment.ChapaCheckoutUrl,
                PaymentReference = payment.PaymentReference,
                Status = payment.Status
            };
        }

        public async Task<bool> VerifyPaymentAsync(string transactionReference)
        {
            // Load payment + booking + event + user
            var payment = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Tickets)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Event)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(p => p.PaymentReference == transactionReference);

            if (payment == null)
                throw new KeyNotFoundException("Payment reference not found.");

            if (payment.Status == PaymentStatus.Paid)
                return true;

            // Verify from Chapa
            var apiKey = _configuration["Chapa:SecretKey"];
            var baseUrl = _configuration["Chapa:BaseUrl"];

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}transaction/verify/{transactionReference}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var responseMessage = await _httpClient.SendAsync(request);
            var response = await responseMessage.Content.ReadFromJsonAsync<ChapaVerifyResponse>();

            if (response == null ||
                response.Status?.ToLower() != "success" ||
                response.Data?.Status?.ToLower() != "success")
            {
                payment.Status = PaymentStatus.Failed;
                await _context.SaveChangesAsync();
                return false;
            }

            // ===== TRANSACTION START =====
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                payment.Status = PaymentStatus.Paid;
                payment.PaidAt = DateTime.UtcNow;

                if (payment.Booking != null)
                {
                    payment.Booking.Status = BookingStatus.Confirmed;

                    await _ticketService.UpdateTicketsStatusAsync(
                        payment.Booking.Id,
                        TicketStatus.Reserved);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            // ===== TRANSACTION END =====


            var emailDto = new BookingEmailDto
            {
                BookingId = payment.Booking.Id,
                UserEmail = payment.Booking.User.Email,
                UserFullName = payment.Booking.User.FullName,
                EventTitle = payment.Booking.Event.Title,
                EventStartDate = payment.Booking.Event.StartDate,
                EventLocation = payment.Booking.Event.Location,
                TotalAmount = payment.Booking.TotalAmount
            };

            var userId = payment.Booking.UserId;
            var eventTitle = payment.Booking.Event.Title;

            //for non blocking task  background task queue to send email and notification with scope services
            _backgroundTaskQueue.QueueBackgroundWorkItem(
        async (sp, ct) =>
        {
            try
            {
                var bookingService = sp.GetRequiredService<IBookingService>();
                var notificationService = sp.GetRequiredService<INotificationService>();

                // Send confirmation email
                await bookingService.SendBookingConfirmationEmailAsync(emailDto);

                // Send notification
                await notificationService.SendNotificationAsync(
                    userId,
                    "Payment Successful",
                    $"Your payment for booking {eventTitle} has been received successfully.",
                    NotificationType.BookingConfirmed);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Background email/notification failed: {ex.Message}");
            }
        }
    );


            return true;
        }


        //for admin or organizer if they want to see all user payments and also want to verify the payment if update status if failed
        public async Task<List<UserPaymentsDto>> GetUsersPaymants()
        {
            var userPayments = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)  // Include User through Booking
                .Select(p => new UserPaymentsDto
                {

                    userId = p.Booking.User.Id,
                    Fullname = p.Booking.User.FullName,
                    Email = p.Booking.User.Email,


                    paymentId = p.Id,
                    BookingId = p.BookingId,
                    Amount = p.Amount,
                    PaymentStatus = p.Status.ToString(),
                    paymentReferenceNumber = p.PaymentReference,
                    checkOutUrl=p.ChapaCheckoutUrl ?? "Not generated or Already paied",
                    PaymentDate = p.PaidAt ?? p.CreatedAt,
                    BookingStatus = p.Booking.Status.ToString(),


                    EventTitle = p.Booking.Event.Title,
                    EventDate = p.Booking.Event.StartDate
                })
                .OrderByDescending(p => p.PaymentDate).ToListAsync();

            return userPayments;
        }


        // creadit service payment logics 


        public async Task<PaymentResultDto> InitializeCreditPurchaseAsync(Guid creditTransactionId)
        {
            
            var tx = await _context.CreditTransactions
                .FirstOrDefaultAsync(t => t.Id == creditTransactionId);

            if (tx == null)
                throw new KeyNotFoundException("Transaction not found.");

            if (tx.Status != PaymentStatus.Pending)
                throw new InvalidOperationException("Transaction already completed or invalid.");

            // 2. Load organizer
            var organizer = await _context.OrganizerProfiles
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == tx.OrganizerProfileId);

            if (organizer == null)
                throw new KeyNotFoundException("Organizer not found.");

            // 3. Generate payment reference HERE
            var paymentReference = $"SE-CREDIT-{DateTime.UtcNow:yyyyMMdd}-{creditTransactionId.ToString("N")[..8].ToUpper()}";

            // 4. Prepare Chapa request
            var baseUrl = _configuration["Chapa:BaseUrl"];
            var callbackUrl = _configuration["Chapa:CreditCallbackUrl"];

            var chapaRequest = new
            {
                amount = tx.TotalAmount,
                currency = "ETB",
                email = organizer.User.Email,
                first_name = organizer.User.FullName,
                phone_number = SanitizePhoneNumber(organizer.User.PhoneNumber),
                tx_ref = paymentReference,
                callback_url = callbackUrl
            };

            var apiKey = _configuration["Chapa:SecretKey"];
            
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}transaction/initialize")
            {
                Content = JsonContent.Create(chapaRequest)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.SendAsync(httpRequest);

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Chapa error: {content}");

            // 5. Parse response
            var json = JsonDocument.Parse(content);
            var checkoutUrl = json.RootElement
                .GetProperty("data")
                .GetProperty("checkout_url")
                .GetString();

            // 6. Save payment info
            tx.PaymentReference = paymentReference;
            tx.ChapaCheckoutUrl = checkoutUrl;

            await _context.SaveChangesAsync();

            return new PaymentResultDto
            {
                PaymentId = tx.Id,
                CheckoutUrl = checkoutUrl!,
                PaymentReference = paymentReference,
                Status = tx.Status
            };
        }





        public async Task<bool> ProcessCreditCallbackAsync(string txRef)
        {

            var ispendingTransaction = await _context.CreditTransactions.FirstOrDefaultAsync(c => c.PaymentReference == txRef);

            if (ispendingTransaction == null)
                throw new KeyNotFoundException("Payment reference not found.");


            if (ispendingTransaction.Status == PaymentStatus.Paid)
                return true;

            // Verify with Chapa
            var apiKey = _configuration["Chapa:SecretKey"];
            var baseUrl = _configuration["Chapa:BaseUrl"];

            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}transaction/verify/{txRef}");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return false;

            var json = JsonDocument.Parse(content);
            var status = json.RootElement.GetProperty("data").GetProperty("status").GetString();

            if (status != "success")
                return false;

            // Mark transaction as completed
            return await _creditService.MarkTransactionCompletedAsync(txRef);
        }



        #region Get pendings to Retry Using The BackgroundJob Scheduler

        public async Task<List<Payment>> GetPendingBookingPaymentsAsync()
        {
            return await _context.Payments
                .Where(p => p.Status == PaymentStatus.Pending)
                .ToListAsync();
        }

        public async Task<List<CreditTransaction>> GetPendingCreditTransactionsAsync()
        {
            return await _context.CreditTransactions
                .Where(t => t.Status == PaymentStatus.Pending)
                .ToListAsync();
        }


        #endregion
        
        private string SanitizePhoneNumber(string? phoneNumber)
        {
            const string fallback = "0912345678";
            
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return fallback;

            // Extract only digits
            var digitsOnly = Regex.Replace(phoneNumber, @"\D", "");

            // Handle international format 251...
            if (digitsOnly.StartsWith("251") && digitsOnly.Length == 12)
            {
                digitsOnly = "0" + digitsOnly.Substring(3);
            }

            // Must start with 09 or 07 and be 10 digits long
            if (digitsOnly.Length == 10 && (digitsOnly.StartsWith("09") || digitsOnly.StartsWith("07")))
            {
                return digitsOnly;
            }

            return fallback;
        }
    }
}

