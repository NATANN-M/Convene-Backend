using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.Booking;
using Convene.Application.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Convene.API.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    [Authorize]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpPost("create-booking")]
        public async Task<IActionResult> CreateBooking([FromBody] BookingCreateDto dto)
        {
            var userIdclaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdclaim))
                return Unauthorized(new { message = "Unauthorized Login and Try again." });
            Guid userId = Guid.Parse(userIdclaim);
            var result = await _bookingService.CreateBookingAsync(dto ,userId);
            return Ok(result);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyBookings()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { message = "Unauthorized Login and Try again." });

            Guid userId = Guid.Parse(userIdClaim);

            var bookings = await _bookingService.GetMyBookingsAsync(userId);
            return Ok(bookings);
        }

        [HttpGet("tickets/{bookingId}")]
        public async Task<IActionResult> GetTicketsForBooking(Guid bookingId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { message = "Unauthorized Login and Try again." });

            Guid userId = Guid.Parse(userIdClaim);

            try
            {
                var tickets = await _bookingService.GetTicketsForBookingAsync(bookingId, userId);
                return Ok(tickets);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

            [HttpGet("get-bookings-by-user/{userId}")]
        public async Task<IActionResult> GetUserBookings(Guid userId)
        {
            var bookings = await _bookingService.GetUserBookingsAsync(userId);
            return Ok(bookings);
        }

        [HttpGet("get-booking-by-id/{bookingId}")]
        public async Task<IActionResult> GetBookingById(Guid bookingId)
        {
            var booking = await _bookingService.GetBookingByIdAsync(bookingId);
            if (booking == null)
                return NotFound();

            return Ok(booking);
        }


        [HttpPut("cancel/{bookingId}")]
          [Authorize] // user must be logged in
        public async Task<IActionResult> CancelBooking(Guid bookingId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized("Unauthorized Login and Try again.");

                Guid userId = Guid.Parse(userIdClaim);

                bool result = await _bookingService.CancelBookingAsync(bookingId, userId);

                if (result)
                    return Ok(new { message = "Booking cancelled successfully." });

                return BadRequest(new { message = "Unable to cancel booking." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
