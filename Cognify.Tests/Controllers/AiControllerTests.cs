using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cognify.Server;
using Cognify.Server.Data;
using Cognify.Server.Dtos.Auth;
using Cognify.Server.DTOs;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Cognify.Tests.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Cognify.Tests.Controllers;

public class AiControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IAiService> _aiServiceMock;

    public AiControllerTests(WebApplicationFactory<Program> factory)
    {
        _aiServiceMock = new Mock<IAiService>();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("TestId", Guid.NewGuid().ToString());
            builder.UseSetting("Jwt:Key", "SuperSecretKeyForIntegrationTesting123!");
            builder.UseSetting("Jwt:Issuer", "TestIssuer");
            builder.UseSetting("Jwt:Audience", "TestAudience");
            builder.UseSetting("OpenAI:ApiKey", "fake-key-for-testing");

            builder.ConfigureTestServices(services =>
            {
                services.AddSqliteTestDatabase<ApplicationDbContext>();
                services.AddScoped(_ => _aiServiceMock.Object); // Replace IAiService with Mock
            });
        });
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private async Task<(HttpClient Client, string Token, Guid UserId)> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var registerDto = new RegisterDto 
        { 
            Email = $"user-{Guid.NewGuid()}@example.com", 
            Password = "password123" 
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", registerDto);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse!.Token);
        
        // Get User Id from Db
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = db.Users.First(u => u.Email == registerDto.Email);

        return (client, authResponse.Token, user.Id);
    }

    [Fact]
    public async Task GenerateQuestions_ShouldReturnQuestions_WhenAuthorizedAndNoteExists()
    {
        // Arrange
        var (client, _, userId) = await CreateAuthenticatedClientAsync();
        
        // Seed Note
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var module = new Module { Id = Guid.NewGuid(), OwnerUserId = userId, Title = "M", Description = "D" };
            var note = new Note { Id = Guid.NewGuid(), ModuleId = module.Id, Title = "Note", Content = "Note Content" };
            db.Modules.Add(module);
            db.Notes.Add(note);
            await db.SaveChangesAsync();
            
            _aiServiceMock.Setup(s => s.GenerateQuestionsFromNoteAsync("Note Content", 5))
                .ReturnsAsync([new QuestionDto { Prompt = "Generated Q" }]);
                
            // Act
            var response = await client.PostAsync($"/api/ai/questions/from-note/{note.Id}", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var questions = await response.Content.ReadFromJsonAsync<List<QuestionDto>>();
            questions.Should().HaveCount(1);
            questions![0].Prompt.Should().Be("Generated Q");
        }
    }

    [Fact]
    public async Task GenerateQuestions_ShouldReturnNotFound_WhenNoteDoesNotExist()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync();
        var response = await client.PostAsync($"/api/ai/questions/from-note/{Guid.NewGuid()}", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
