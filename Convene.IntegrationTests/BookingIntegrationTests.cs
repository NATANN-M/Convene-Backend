using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Convene.Application.DTOs.Booking;
using Convene.Application.DTOs.Requests;
using Xunit;

namespace Convene.IntegrationTests
{
    public class BookingIntegrationTests : IClassFixture<CustomWebApplicationFactory<Convene.API.Controllers.AuthController>>
    {
        private readonly CustomWebApplicationFactory<Convene.API.Controllers.AuthController> _factory;

        public BookingIntegrationTests(CustomWebApplicationFactory<Convene.API.Controllers.AuthController> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateBooking_AsAuthenticatedAttendee_ShouldReturnOk()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // 1. Seed an Event
            var (eventId, ticketTypeId) = await _factory.SeedEventWithTicketsAsync();

            // 2. Register & Login as Attendee
            var email = "attendee@test.com";
            var password = "Password123!";
            await client.PostAsJsonAsync("/api/Auth/register-Attendee", new RegisterUserRequest
            {
                FullName = "Test Attendee",
                Email = email,
                PhoneNumber = "0911223344",
                Password = password
            });
            var token = await _factory.GetAuthTokenAsync(email, password);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 3. Create Booking
            var bookingRequest = new BookingCreateDto
            {
                EventId = eventId,
                Tickets = new List<TicketCreateDto>
                {
                    new TicketCreateDto
                    {
                        TicketTypeId = ticketTypeId,
                        Quantity = 2,
                        HolderName = "Test Guest",
                        HolderPhone = "0911556677"
                    }
                }
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/bookings/create-booking", bookingRequest);

            // Assert
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Create Booking failed with {response.StatusCode}: {error}");
            }
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            result.GetProperty("bookingId").GetGuid().Should().NotBeEmpty();
        }
    }
}
