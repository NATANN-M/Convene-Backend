using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Convene.Application.DTOs.Event;
using Convene.Domain.Entities;
using Convene.Domain.Enums;
using Convene.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Convene.IntegrationTests
{
    public class EventManagementIntegrationTests : IClassFixture<CustomWebApplicationFactory<Convene.API.Controllers.AuthController>>
    {
        private readonly CustomWebApplicationFactory<Convene.API.Controllers.AuthController> _factory;

        public EventManagementIntegrationTests(CustomWebApplicationFactory<Convene.API.Controllers.AuthController> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateEvent_AsAuthorizedOrganizer_ShouldReturnOk()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            // 1. Register an Organizer
            var organizerEmail = "organizer@test.com";
            var password = "Password123!";
            
            using var regContent = new MultipartFormDataContent();
            regContent.Add(new StringContent("Test Organizer"), "FullName");
            regContent.Add(new StringContent(organizerEmail), "Email");
            regContent.Add(new StringContent("0987654321"), "PhoneNumber");
            regContent.Add(new StringContent(password), "Password");
            regContent.Add(new StringContent("Test Business"), "BusinessName");
            regContent.Add(new StringContent("business@test.com"), "BusinessEmail");
            
            // Dummy KYC images
            var byteFile = new byte[] { 0x20, 0x20, 0x20 };
            var kycFront = new ByteArrayContent(byteFile);
            kycFront.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            regContent.Add(kycFront, "KYCFrontImage", "front.jpg");
            
            var kycBack = new ByteArrayContent(byteFile);
            kycBack.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            regContent.Add(kycBack, "KYCBackImage", "back.jpg");

            var regResponse = await client.PostAsync("/api/Auth/register-organizer", regContent);
            if (regResponse.StatusCode != HttpStatusCode.OK)
            {
                var error = await regResponse.Content.ReadAsStringAsync();
                throw new Exception($"Register Organizer failed with {regResponse.StatusCode}: {error}");
            }
            regResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Manually verify organizer in DB to allow login
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ConveneDbContext>();
                var user = await db.Users.Include(u => u.OrganizerProfile).FirstOrDefaultAsync(u => u.Email == organizerEmail);
                if (user != null)
                {
                    user.Status = UserStatus.Active;
                    user.OrganizerProfile.IsVerified = true;
                    await db.SaveChangesAsync();
                }
            }

            // 2. Get Token
            var token = await _factory.GetAuthTokenAsync(organizerEmail, password);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 3. Create Event
            using var eventContent = new MultipartFormDataContent();
            eventContent.Add(new StringContent("Integration Test Event"), "Title");
            eventContent.Add(new StringContent("This is a test event description."), "Description");
            eventContent.Add(new StringContent("00000000-0000-0000-0000-000000000001"), "CategoryId"); // Seeded ID
            eventContent.Add(new StringContent("Test Venue"), "Venue");
            eventContent.Add(new StringContent("Test Location"), "Location");
            eventContent.Add(new StringContent(DateTime.UtcNow.AddDays(1).ToString("o")), "TicketSalesStart");
            eventContent.Add(new StringContent(DateTime.UtcNow.AddDays(7).ToString("o")), "TicketSalesEnd");
            eventContent.Add(new StringContent(DateTime.UtcNow.AddDays(8).ToString("o")), "StartDate");
            eventContent.Add(new StringContent(DateTime.UtcNow.AddDays(9).ToString("o")), "EndDate");
            eventContent.Add(new StringContent("100"), "TotalCapacity");

            var coverImage = new ByteArrayContent(byteFile);
            coverImage.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            eventContent.Add(coverImage, "CoverImage", "cover.jpg");

            // Act
            var response = await client.PostAsync("/api/events/create-event", eventContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            result.GetProperty("title").GetString().Should().Be("Integration Test Event");
        }
    }
}
