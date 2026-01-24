using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Convene.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Linq;
using System;
using Moq;
using Convene.Application.Interfaces;
using Convene.Application.Interfaces.Convene.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Convene.IntegrationTests
{
    public class CustomWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Override the connection string to use a dedicated test database
                // This avoids provider conflicts and tests against the real PostgreSQL
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=Convene_IntegrationTests;Username=postgres;Password=123"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Mock Cloudinary
                var mockCloudinary = new Mock<ICloudinaryService>();
                mockCloudinary.Setup(x => x.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                    .ReturnsAsync("https://test-cloudinary.com/image.jpg");

                services.AddSingleton(mockCloudinary.Object);

                // Mock Email
                var mockEmail = new Mock<IEmailService>();
                mockEmail.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.CompletedTask);

                services.AddSingleton(mockEmail.Object);

                // Disable Background Services
                var backgroundServices = services.Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToList();
                foreach (var service in backgroundServices)
                {
                    services.Remove(service);
                }

                // 4. Initialize the database and SEED data
                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ConveneDbContext>();

                    // WARNING: This will drop and recreate the IntegrationTests database
                    try
                    {
                        db.Database.EnsureDeleted();
                    }
                    catch (Npgsql.PostgresException ex) when (ex.SqlState == "3D000")
                    {
                        // Database does not exist, ignore
                    }
                    catch (Exception)
                    {
                        // Other connection issues might happen, but we'll try Migrate next
                    }

                    try
                    {
                        db.Database.Migrate();
                    }
                    catch (Exception ex)
                    {
                        // If migrate fails, it might be because the DB doesn't exist and couldn't be created 
                        // via the current connection string. Fallback to EnsureCreated for testing if needed.
                        // But Migrate is preferred.
                        throw new Exception($"Failed to migrate database: {ex.Message}", ex);
                    }

                    // Seed a default category
                    if (!db.EventCategories.Any())
                    {
                        db.EventCategories.Add(new Domain.Entities.EventCategory
                        {
                            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                            Name = "Test Category"
                        });
                        db.SaveChanges();
                    }
                }
            });
        }

        public async Task<string> GetAuthTokenAsync(string email, string password, string role = "Attendee")
        {
            using var scope = Services.CreateScope();
            var client = CreateClient();

            // Login
            var loginResponse = await client.PostAsJsonAsync("/api/Auth/login", new { Email = email, Password = password });
            var result = await loginResponse.Content.ReadFromJsonAsync<Convene.Application.DTOs.Responses.AuthResponse>();
            return result?.Token ?? string.Empty;
        }
        public async Task<(Guid eventId, Guid ticketTypeId)> SeedEventWithTicketsAsync()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ConveneDbContext>();

            var categoryId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            var ev = new Domain.Entities.Event
            {
                Id = Guid.NewGuid(),
                Title = "Seeded Test Event",
                CategoryId = categoryId,
                TicketSalesStart = DateTime.UtcNow.AddDays(-1),
                TicketSalesEnd = DateTime.UtcNow.AddDays(10),
                StartDate = DateTime.UtcNow.AddDays(10),
                EndDate = DateTime.UtcNow.AddDays(11),
                TotalCapacity = 1000,
                Status = Domain.Enums.EventStatus.Published
            };

            var ticketType = new Domain.Entities.TicketType
            {
                Id = Guid.NewGuid(),
                EventId = ev.Id,
                Name = "General Admission",
                BasePrice = 50.0m,
                Quantity = 100,
                Sold = 0,
                IsActive = true
            };

            db.Events.Add(ev);
            db.TicketTypes.Add(ticketType);
            await db.SaveChangesAsync();

            return (ev.Id, ticketType.Id);
        }
    }
}
