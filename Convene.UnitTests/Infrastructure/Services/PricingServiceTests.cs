using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.Event;
using Convene.Domain.Entities;
using Convene.Domain.Enums;
using Convene.Infrastructure.Persistence;
using Convene.Infrastructure.Services;
using Xunit;

namespace Convene.UnitTests.Infrastructure.Services
{
    public class PricingServiceTests
    {
        private readonly ConveneDbContext _context;
        private readonly PricingService _pricingService;

        public PricingServiceTests()
        {
            var options = new DbContextOptionsBuilder<ConveneDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new ConveneDbContext(options);
            _pricingService = new PricingService(_context);
        }

        [Fact]
        public async Task GetCurrentPrice_BasePriceOnly_ReturnsBasePrice()
        {
            // Arrange
            var ticketTypeId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var eventEntity = new Event { Id = eventId, Title = "Test Event" };
            _context.Events.Add(eventEntity);

            var ticketType = new TicketType
            {
                Name = "General",
                Id = ticketTypeId,
                BasePrice = 100,
                EventId = eventId,
                PricingRules = new List<DynamicPricingRule>()
            };
            _context.TicketTypes.Add(ticketType);
            await _context.SaveChangesAsync();

            // Act
            var price = await _pricingService.GetCurrentPriceAsync(ticketTypeId);

            // Assert
            price.Should().Be(100);
        }

        [Fact]
        public async Task GetCurrentPrice_EarlyBirdActive_ReturnsDiscountedPrice()
        {
            // Arrange
            var ticketTypeId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var eventId = Guid.NewGuid();
            var eventEntity = new Event { Id = eventId, Title = "Test Event" };
            _context.Events.Add(eventEntity);
            
            var ticketType = new TicketType
            {
                Name = "General",
                Id = ticketTypeId,
                BasePrice = 100,
                EventId = eventId,
                PricingRules = new List<DynamicPricingRule>
                {
                    new DynamicPricingRule
                    {
                        RuleType = PricingRuleType.EarlyBird,
                        IsActive = true,
                        StartDate = now.AddDays(-1),
                        EndDate = now.AddDays(1),
                        DiscountPercent = 20
                    }
                }
            };
            _context.TicketTypes.Add(ticketType);
            await _context.SaveChangesAsync();

            // Act
            var price = await _pricingService.GetCurrentPriceAsync(ticketTypeId);

            // Assert
            price.Should().Be(80); // 100 - 20%
        }

        [Fact]
        public async Task GetCurrentPrice_LastMinuteActive_ReturnsDiscountedPrice()
        {
            // Arrange
            var ticketTypeId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var eventEntity = new Event
            {
                Title = "Test Event",
                Id = eventId,
                StartDate = now.AddDays(2) // Event is in 2 days
            };

            var ticketType = new TicketType
            {
                Name = "General",
                Id = ticketTypeId,
                BasePrice = 100,
                EventId = eventId,
                Event = eventEntity,
                PricingRules = new List<DynamicPricingRule>
                {
                    new DynamicPricingRule
                    {
                        RuleType = PricingRuleType.LastMinute,
                        IsActive = true,
                        LastNDaysBeforeEvent = 3, // Rule applies 3 days before
                        DiscountPercent = 10
                    }
                }
            };
            _context.TicketTypes.Add(ticketType);
            _context.Events.Add(eventEntity);
            await _context.SaveChangesAsync();

            // Act
            var price = await _pricingService.GetCurrentPriceAsync(ticketTypeId);

            // Assert
            price.Should().Be(90); // 100 - 10%
        }

        [Fact]
        public async Task GetCurrentPrice_DemandBased_ThresholdReached_ReturnsIncreasedPrice()
        {
            // Arrange
            var ticketTypeId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var eventEntity = new Event { Id = eventId, Title = "Test Event" };
            _context.Events.Add(eventEntity);
            
            var ticketType = new TicketType
            {
                Name = "General",
                Id = ticketTypeId,
                BasePrice = 100,
                EventId = eventId,
                Quantity = 100,
                Sold = 80, // 80% sold
                PricingRules = new List<DynamicPricingRule>
                {
                    new DynamicPricingRule
                    {
                        RuleType = PricingRuleType.DemandBased,
                        IsActive = true,
                        ThresholdPercentage = 75,
                        PriceIncreasePercent = 50
                    }
                }
            };
            _context.TicketTypes.Add(ticketType);
            await _context.SaveChangesAsync();

            // Act
            var price = await _pricingService.GetCurrentPriceAsync(ticketTypeId);

            // Assert
            price.Should().Be(150); // 100 + 50%
        }

        [Fact]
        public void ValidatePricingRule_InvalidDates_ThrowsException()
        {
            // Arrange
            var dto = new PricingRuleCreateDto
            {
                RuleType = PricingRuleType.EarlyBird,
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow, // End before Start
                DiscountPercent = 10
            };

            // Act
            Action act = () => PricingService.ValidatePricingRule(dto);

            // Assert
            act.Should().Throw<Exception>().WithMessage("EndDate must be after StartDate.");
        }
    }
}
