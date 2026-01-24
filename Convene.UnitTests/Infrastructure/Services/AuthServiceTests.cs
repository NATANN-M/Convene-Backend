using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Convene.Application.DTOs.Auth;
using Convene.Application.DTOs.Requests;
using Convene.Application.Interfaces;
using Convene.Application.Interfaces.Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Domain.Enums;
using Convene.Infrastructure.Helpers;
using Convene.Infrastructure.Persistence;
using Convene.Infrastructure.Services;
using Xunit;

namespace Convene.UnitTests.Infrastructure.Services
{
    public class AuthServiceTests
    {
        private readonly ConveneDbContext _context;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<ICloudinaryService> _cloudinaryServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly PasswordHasher _passwordHasher;
        private readonly JwtService _jwtService;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            var options = new DbContextOptionsBuilder<ConveneDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ConveneDbContext(options);
            _emailServiceMock = new Mock<IEmailService>();
            _cloudinaryServiceMock = new Mock<ICloudinaryService>();

            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(x => x["Jwt:Secret"]).Returns("super_secret_key_for_testing_purposes_only");
            _configurationMock.Setup(x => x["Jwt:Issuer"]).Returns("TestIssuer");
            _configurationMock.Setup(x => x["Jwt:Audience"]).Returns("TestAudience");

            _passwordHasher = new PasswordHasher();
            _jwtService = new JwtService(_configurationMock.Object);

            _authService = new AuthService(
                _context,
                _passwordHasher,
                _jwtService,
                _emailServiceMock.Object,
                _cloudinaryServiceMock.Object
            );
        }

        [Fact]
        public async Task RegisterAsync_EmailExists_ReturnsFailure()
        {
            // Arrange
            var email = "existing@example.com";
            _context.Users.Add(new User
            {
                Email = email,
                PasswordHash = "hash",
                FullName = "Existing User",
                PhoneNumber = "0912345678"
            });
            await _context.SaveChangesAsync();

            var request = new RegisterUserRequest { Email = email, Password = "Password123" };

            // Act
            var response = await _authService.RegisterAsync(request);

            // Assert
            response.Success.Should().BeFalse();
            response.Message.Should().Be("Email already exists");
        }

        [Fact]
        public async Task RegisterAsync_Success_ReturnsToken()
        {
            // Arrange
            var request = new RegisterUserRequest
            {
                Email = "new@example.com",
                Password = "Password123",
                FullName = "New User",
                PhoneNumber = "0912345678"
            };

            // Act
            var response = await _authService.RegisterAsync(request);

            // Assert
            response.Success.Should().BeTrue();
            response.Token.Should().NotBeNullOrEmpty();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            user.Should().NotBeNull();
            _passwordHasher.VerifyPassword(request.Password, user.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public async Task LoginAsync_InvalidCredentials_ReturnsFailure()
        {
            // Arrange
            var email = "user@example.com";
            var password = "Password123";
            _context.Users.Add(new User
            {
                Email = email,
                PasswordHash = _passwordHasher.HashPassword(password),
                Status = UserStatus.Active,
                Role = UserRole.Attendee,
                FullName = "User Login",
                PhoneNumber = "0912345678"
            });
            await _context.SaveChangesAsync();

            var request = new LoginUserRequest { Email = email, Password = "WrongPassword" };

            // Act
            var response = await _authService.LoginAsync(request);

            // Assert
            response.Success.Should().BeFalse();
            response.Message.Should().Be("Invalid email or password");
        }

        [Fact]
        public async Task LoginAsync_OrganizerUnverified_ReturnsFailure()
        {
            // Arrange
            var email = "organizer@example.com";
            var user = new User
            {
                Email = email,
                PasswordHash = _passwordHasher.HashPassword("Password123"),
                Role = UserRole.Organizer,
                Status = UserStatus.Pending,
                FullName = "Unverified Org",
                PhoneNumber = "0912345678"
            };
            user.OrganizerProfile = new OrganizerProfile { IsVerified = false };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new LoginUserRequest { Email = email, Password = "Password123" };

            // Act
            var response = await _authService.LoginAsync(request);

            // Assert
            response.Success.Should().BeFalse();
            response.Message.Should().Contain("Organizer Account not authorized");
        }

        [Fact]
        public async Task RegisterOrganizerAsync_Success_ReturnsSuccessAndPendingStatus()
        {
            // Arrange
            var request = new RegisterOrganizerRequest
            {
                Email = "org@example.com",
                Password = "Password123",
                FullName = "Organizer Name",
                PhoneNumber = "0912345678",
                BusinessName = "Biz Inc",
                BusinessEmail = "biz@example.com",
                KYCFrontImage = new Mock<IFormFile>().Object,
                KYCBackImage = new Mock<IFormFile>().Object
            };

            _cloudinaryServiceMock.Setup(x => x.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ReturnsAsync("http://cloudinary.com/image.jpg");

            // Act
            var response = await _authService.RegisterOrganizerAsync(request);

            // Assert
            response.Success.Should().BeTrue();
            response.Message.Should().Contain("pending admin approval");

            var user = await _context.Users.Include(u => u.OrganizerProfile).FirstOrDefaultAsync(u => u.Email == request.Email);
            user.Should().NotBeNull();
            user.Role.Should().Be(UserRole.Organizer);
            user.Status.Should().Be(UserStatus.Pending);
            user.OrganizerProfile.Should().NotBeNull();
            user.OrganizerProfile.BusinessName.Should().Be("Biz Inc");
        }
    }
}
