using System.Net;
using System.Net.Http.Json;
using Cognify.Server.Data;
using Cognify.Server.Dtos.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using Cognify.Server;
using Microsoft.AspNetCore.Hosting; // Computed missing using
using Cognify.Tests.Extensions;

namespace Cognify.Tests.Controllers;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            // Add a random setting to force the Host to reload, ensuring a fresh DB for each test
            builder.UseSetting("TestId", Guid.NewGuid().ToString());
            builder.UseSetting("Jwt:Key", "SuperSecretKeyForIntegrationTesting123!");
            builder.UseSetting("Jwt:Issuer", "TestIssuer");
            builder.UseSetting("Jwt:Audience", "TestAudience");

            builder.ConfigureServices(services =>
            {
                services.AddSqliteTestDatabase<ApplicationDbContext>();
            });
        });
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    [Fact]
    public async Task Register_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var client = _factory.CreateClient();
        var dto = new RegisterDto { Email = "newuser@example.com", Password = "password123" };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        result.Should().NotBeNull();
        result!.Email.Should().Be(dto.Email);
        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_ShouldReturnConflict_WhenUserAlreadyExists()
    {
        // Arrange
        var client = _factory.CreateClient();
        var dto = new RegisterDto { Email = "conflict@example.com", Password = "password123" };

        // First registration
        await client.PostAsJsonAsync("/api/auth/register", dto);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_ShouldReturnOk_WhenCredentialsAreValid()
    {
        // Arrange
        var client = _factory.CreateClient();
        var registerDto = new RegisterDto { Email = "loginuser@example.com", Password = "password123" };
        await client.PostAsJsonAsync("/api/auth/register", registerDto);

        var loginDto = new LoginDto { Email = "loginuser@example.com", Password = "password123" };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        result.Should().NotBeNull();
        result!.Email.Should().Be(loginDto.Email);
        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginDto = new LoginDto { Email = "nonexistent@example.com", Password = "password123" };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    [Fact]
    public async Task UpdateProfile_ShouldReturnOk_WhenAuthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var registerDto = new RegisterDto { Email = "profileuser@example.com", Password = "password123", Username = "OldName" };
        var authResponse = await client.PostAsJsonAsync("/api/auth/register", registerDto);
        var authResult = await authResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult!.Token);

        var updateDto = new UpdateProfileDto { Email = "profileuser@example.com", Username = "NewName" };

        // Act
        var response = await client.PutAsJsonAsync("/api/auth/update-profile", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        result.Should().NotBeNull();
        result!.Username.Should().Be("NewName");
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnNoContent_WhenAuthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var registerDto = new RegisterDto { Email = "pwuser@example.com", Password = "password123" };
        var authResponse = await client.PostAsJsonAsync("/api/auth/register", registerDto);
        var authResult = await authResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult!.Token);

        var changePwDto = new ChangePasswordDto { CurrentPassword = "password123", NewPassword = "newpassword123" };

        // Act
        var response = await client.PutAsJsonAsync("/api/auth/change-password", changePwDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Me_ShouldReturnProfile_WhenAuthorized()
    {
        var client = _factory.CreateClient();
        var registerDto = new RegisterDto { Email = "meuser@example.com", Password = "password123" };
        var authResponse = await client.PostAsJsonAsync("/api/auth/register", registerDto);
        var authResult = await authResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult!.Token);

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        profile.Should().NotBeNull();
        profile!.Email.Should().Be(registerDto.Email);
    }
}
