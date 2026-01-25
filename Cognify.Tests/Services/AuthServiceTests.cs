using Cognify.Server.Data;
using Cognify.Server.Dtos.Auth;
using Cognify.Server.Models;
using Cognify.Server.Services;
using Cognify.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Cognify.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _configurationMock = new Mock<IConfiguration>();

        // Mock Configuration for JWT
        var jwtSectionMock = new Mock<IConfigurationSection>();
        jwtSectionMock.Setup(s => s["Key"]).Returns("SuperSecretKeyForTestingTheAuthService123!");
        jwtSectionMock.Setup(s => s["Issuer"]).Returns("TestIssuer");
        jwtSectionMock.Setup(s => s["Audience"]).Returns("TestAudience");
        _configurationMock.Setup(c => c.GetSection("Jwt")).Returns(jwtSectionMock.Object);

        _authService = new AuthService(_context, _passwordHasherMock.Object, _configurationMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnAuthResponse_WhenUserDoesNotExist()
    {
        // Arrange
        var dto = new RegisterDto { Email = "test@example.com", Password = "password123" };
        _passwordHasherMock.Setup(ph => ph.HashPassword(dto.Password)).Returns("hashed_password");

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(dto.Email);
        result.Token.Should().NotBeNullOrEmpty();
        
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        user.Should().NotBeNull();
        user!.PasswordHash.Should().Be("hashed_password");
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowInvalidOperationException_WhenUserAlreadyExists()
    {
        // Arrange
        var existingUser = new User { Email = "existing@example.com", PasswordHash = "hash" };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var dto = new RegisterDto { Email = "existing@example.com", Password = "newpassword" };

        // Act
        Func<Task> act = async () => await _authService.RegisterAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User with this email already exists.");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnAuthResponse_WhenCredentialsAreValid()
    {
        // Arrange
        var email = "user@example.com";
        var password = "password123";
        var hash = "hashed_password";

        var user = new User { Email = email, PasswordHash = hash };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var dto = new LoginDto { Email = email, Password = password };

        _passwordHasherMock.Setup(ph => ph.VerifyPassword(password, hash)).Returns(true);

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(email);
        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorizedAccessException_WhenUserNotFound()
    {
        // Arrange
        var dto = new LoginDto { Email = "nonexistent@example.com", Password = "password" };

        // Act
        Func<Task> act = async () => await _authService.LoginAsync(dto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorizedAccessException_WhenPasswordIsInvalid()
    {
        // Arrange
        var email = "user@example.com";
        var password = "wrongpassword";
        var hash = "hashed_password";

        var user = new User { Email = email, PasswordHash = hash };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var dto = new LoginDto { Email = email, Password = password };

        _passwordHasherMock.Setup(ph => ph.VerifyPassword(password, hash)).Returns(false);

        // Act
        Func<Task> act = async () => await _authService.LoginAsync(dto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials.");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
