using System.Net;
using System.Text.Json;

using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cognify.Server;
using Cognify.Server.Data;
using Cognify.Server.Dtos.Auth;
using Cognify.Server.DTOs;
using Cognify.Server.Models;
using Cognify.Server.Models.Ai;
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
    private readonly Mock<IDocumentService> _documentServiceMock;
    private readonly Mock<IBlobStorageService> _blobStorageMock;

    public AiControllerTests(WebApplicationFactory<Program> factory)
    {
        _aiServiceMock = new Mock<IAiService>();
        _documentServiceMock = new Mock<IDocumentService>();
        _blobStorageMock = new Mock<IBlobStorageService>();

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
                services.AddScoped(_ => _aiServiceMock.Object);
                services.AddScoped(_ => _documentServiceMock.Object);
                services.AddScoped(_ => _blobStorageMock.Object);
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
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>(TestConstants.JsonOptions);
        
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
        
        Guid noteId;
        // Seed Note
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var module = new Module { Id = Guid.NewGuid(), OwnerUserId = userId, Title = "M", Description = "D" };
            var note = new Note { Id = Guid.NewGuid(), ModuleId = module.Id, Title = "Note", Content = "Note Content" };
            db.Modules.Add(module);
            db.Notes.Add(note);
            await db.SaveChangesAsync();
            noteId = note.Id;
        }

        _aiServiceMock.Setup(s => s.GenerateQuestionsAsync("Note Content", QuestionType.MultipleChoice, 2, 5))
            .ReturnsAsync(
            [
                new GeneratedQuestion 
                { 
                    Text = "Generated Q", 
                    Type = QuestionType.MultipleChoice,
                    Options = ["A", "B"], 
                    CorrectAnswer = "A" 
                }
            ]);
            
        // Act
        var request = new Cognify.Server.Dtos.Ai.GenerateQuestionsRequest 
        { 
            NoteId = noteId.ToString(),
            Type = QuestionType.MultipleChoice,
            Difficulty = 2,
            Count = 5
        };
        var response = await client.PostAsJsonAsync($"/api/ai/questions/generate", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var questions = await response.Content.ReadFromJsonAsync<List<GeneratedQuestion>>(TestConstants.JsonOptions);
        questions.Should().HaveCount(1);
        questions![0].Text.Should().Be("Generated Q");
    }

    [Fact]
    public async Task Grade_ShouldReturnAnalysis()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync();
        var req = new Cognify.Server.Dtos.Ai.GradeAnswerRequest 
        { 
            Question = "Q", Answer = "A", Context = "C" 
        };

        _aiServiceMock.Setup(s => s.GradeAnswerAsync("Q", "A", "C"))
            .ReturnsAsync("Score: 100");

        var response = await client.PostAsJsonAsync("/api/ai/grade", req);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    [Fact]
    public async Task ExtractText_ShouldReturnAccepted_AndCreatePending_WhenValid()
    {
        // Arrange
        var (client, _, userId) = await CreateAuthenticatedClientAsync();
        
        Guid documentId;
        Guid moduleId;
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var module = new Module { Id = Guid.NewGuid(), OwnerUserId = userId, Title = "M", Description = "D" };
            var doc = new Document { Id = Guid.NewGuid(), ModuleId = module.Id, FileName = "test.png", BlobPath = "path", Status = Cognify.Server.Models.DocumentStatus.Uploaded, CreatedAt = DateTime.UtcNow };
            db.Modules.Add(module);
            db.Documents.Add(doc);
            await db.SaveChangesAsync();
            documentId = doc.Id;
            moduleId = module.Id;
        }

        // Mock Blob Download
        _blobStorageMock.Setup(x => x.DownloadStreamAsync(It.IsAny<string>())).ReturnsAsync(new MemoryStream());

        // Mock Document Service
        var docDto = new Cognify.Server.Dtos.Documents.DocumentDto(documentId, moduleId, "test.png", "path", Cognify.Server.Dtos.Documents.DocumentStatus.Uploaded, DateTime.UtcNow, 1024);
        _documentServiceMock.Setup(dx => dx.GetByIdAsync(documentId)).ReturnsAsync(docDto);

        // Act
        var response = await client.PostAsync($"/api/ai/extract-text/{documentId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var pendingId = content.GetProperty("extractedContentId").GetGuid();
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pending = db.ExtractedContents.FirstOrDefault(x => x.Id == pendingId);
            pending.Should().NotBeNull();
            pending!.Status.Should().Be("Processing");
            pending.DocumentId.Should().Be(documentId);
        }
    }
}
