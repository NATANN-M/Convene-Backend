using Convene.Application.DTOs.Booking;
using Convene.Application.DTOs.Notifications;
using Convene.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Convene.Application.Interfaces
{
    public interface IBookingService
    {
        Task<BookingResultDto> CreateBookingAsync(BookingCreateDto dto,Guid userId);
        Task<List<MyBookingDto>> GetMyBookingsAsync(Guid userId);

        Task<List<TicketViewDto>> GetTicketsForBookingAsync(Guid bookingId, Guid userId);

        Task<List<BookingSummaryDto>> GetUserBookingsAsync(Guid userId);
        Task<BookingSummaryDto?> GetBookingByIdAsync(Guid bookingId);
        Task<bool> CancelBookingAsync(Guid bookingId, Guid userId);
        Task SendBookingConfirmationEmailAsync(BookingEmailDto dto);
    }
}
