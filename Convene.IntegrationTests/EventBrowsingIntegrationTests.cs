using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Convene.Application.DTOs.EventBrowsing;
using Convene.Application.DTOs.PagenationDtos;
using Xunit;

namespace Convene.IntegrationTests
{
    // We target AuthController just as a way to find the API project
    public class EventBrowsingIntegrationTests : IClassFixture<CustomWebApplicationFactory<Convene.API.Controllers.AuthController>>
    {
        private readonly CustomWebApplicationFactory<Convene.API.Controllers.AuthController> _factory;

        public EventBrowsingIntegrationTests(CustomWebApplicationFactory<Convene.API.Controllers.AuthController> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetActiveEvents_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/EventBrowsing/activeEvents");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<PaginatedResult<EventSummaryDto>>();
            result.Should().NotBeNull();
        }
    }
}
