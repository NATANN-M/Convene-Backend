using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Convene.Application.DTOs.Requests;
using Convene.Application.DTOs.Responses;
using Xunit;

namespace Convene.IntegrationTests
{
    public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory<Convene.API.Controllers.AuthController>>
    {
        private readonly CustomWebApplicationFactory<Convene.API.Controllers.AuthController> _factory;

        public AuthIntegrationTests(CustomWebApplicationFactory<Convene.API.Controllers.AuthController> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task RegisterAndLogin_ShouldReturnToken()
        {
            // Arrange
            var client = _factory.CreateClient();
            var registerRequest = new RegisterUserRequest
            {
                FullName = "Integration Test User",
                Email = "testuser@example.com",
                PhoneNumber = "0912345678",
                Password = "Password123!"
            };

            // Act - Part 1: Register
            var regResponse = await client.PostAsJsonAsync("/api/Auth/register-Attendee", registerRequest);
            regResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Act - Part 2: Login
            var loginRequest = new 
            {
                Email = "testuser@example.com",
                Password = "Password123!"
            };
            var loginResponse = await client.PostAsJsonAsync("/api/Auth/login", loginRequest);

            // Assert
            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
            result.Should().NotBeNull();
            result!.Token.Should().NotBeNullOrEmpty();
            result.Email.Should().Be("testuser@example.com");
        }

        [Fact]
        public async Task Login_WithWrongPassword_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();
            var loginRequest = new
            {
                Email = "nonexistent@example.com",
                Password = "WrongPassword"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/Auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
