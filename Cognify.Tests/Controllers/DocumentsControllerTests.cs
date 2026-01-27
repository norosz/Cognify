using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Azure.Storage.Sas;
using Cognify.Server;
using Cognify.Server.Data;
using Cognify.Server.Dtos.Auth;
using Cognify.Server.Dtos.Documents;
using Cognify.Server.Dtos.Modules;
using Cognify.Server.Services.Interfaces;
using Cognify.Tests.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Cognify.Tests.Controllers;

public class DocumentsControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IBlobStorageService> _blobStorageMock;

    public DocumentsControllerTests(WebApplicationFactory<Program> factory)
    {
        _blobStorageMock = new Mock<IBlobStorageService>();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("TestId", Guid.NewGuid().ToString());
            builder.UseSetting("Jwt:Key", "SuperSecretKeyForIntegrationTesting123!");
            builder.UseSetting("Jwt:Issuer", "TestIssuer");
            builder.UseSetting("Jwt:Audience", "TestAudience");

            builder.ConfigureServices(services =>
            {
                services.AddSqliteTestDatabase<ApplicationDbContext>();
                
                // Replace IBlobStorageService with Mock
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBlobStorageService));
                if (descriptor != null) services.Remove(descriptor);
                
                services.AddScoped(_ => _blobStorageMock.Object);
            });
        });
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private async Task<(HttpClient Client, string Token)> CreateAuthenticatedClientAsync()
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

        return (client, authResponse.Token);
    }

    private async Task<ModuleDto> CreateModuleAsync(HttpClient client)
    {
        var dto = new CreateModuleDto { Title = "Test Module", Description = "For Docs" };
        var res = await client.PostAsJsonAsync("/api/modules", dto);
        return (await res.Content.ReadFromJsonAsync<ModuleDto>())!;
    }

    [Fact]
    public async Task InitiateUpload_ShouldReturnSas_WhenValid()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var module = await CreateModuleAsync(client);

        _blobStorageMock
            .Setup(x => x.GenerateUploadSasToken(It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .Returns("https://azurite/container/blob?sas=token");

        var request = new UploadInitiateRequest("test.txt", "text/plain");

        // Act
        var response = await client.PostAsJsonAsync($"/api/modules/{module.Id}/documents/initiate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UploadInitiateResponse>();
        result!.SasUrl.Should().Contain("sas=token");
        result.BlobName.Should().Contain("test.txt");
    }

    [Fact]
    public async Task CompleteUpload_ShouldReturnReady_WhenValid()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var module = await CreateModuleAsync(client);

        _blobStorageMock
            .Setup(x => x.GenerateUploadSasToken(It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .Returns("https://sas-url");
        _blobStorageMock
            .Setup(x => x.GenerateDownloadSasToken(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<string?>()))
            .Returns("https://sas-url");

        // Initiate
        var request = new UploadInitiateRequest("test.txt", "text/plain");
        var initRes = await client.PostAsJsonAsync($"/api/modules/{module.Id}/documents/initiate", request);
        var initData = await initRes.Content.ReadFromJsonAsync<UploadInitiateResponse>();

        // Act
        var response = await client.PostAsync($"/api/documents/{initData!.DocumentId}/complete", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = await response.Content.ReadFromJsonAsync<DocumentDto>();
        doc!.Status.Should().Be(DocumentStatus.Ready);
        doc.DownloadUrl.Should().Be("https://sas-url"); // Should return Read SAS
        
        // Verify NO pending extraction was created (Auto-extraction removed)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.ExtractedContents.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task GetModuleDocuments_ShouldReturnList_WhenDocumentsExist()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var module = await CreateModuleAsync(client);

        _blobStorageMock
            .Setup(x => x.GenerateUploadSasToken(It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .Returns("https://sas-url");
        _blobStorageMock
            .Setup(x => x.GenerateDownloadSasToken(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<string?>()))
            .Returns("https://sas-url");

        // Create a document
        var request = new UploadInitiateRequest("test.txt", "text/plain");
        var initRes = await client.PostAsJsonAsync($"/api/modules/{module.Id}/documents/initiate", request);
        var initData = await initRes.Content.ReadFromJsonAsync<UploadInitiateResponse>();
        await client.PostAsync($"/api/documents/{initData!.DocumentId}/complete", null);

        // Act
        var response = await client.GetAsync($"/api/modules/{module.Id}/documents");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<DocumentDto>>();
        list.Should().HaveCount(1);
        list![0].Status.Should().Be(DocumentStatus.Ready);
        list[0].DownloadUrl.Should().Be("https://sas-url");
    }

    [Fact]
    public async Task DeleteDocument_ShouldDelete_WhenOwner()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var module = await CreateModuleAsync(client);

        _blobStorageMock.Setup(x => x.GenerateUploadSasToken(It.IsAny<string>(), It.IsAny<DateTimeOffset>())).Returns("url");
        _blobStorageMock.Setup(x => x.DeleteAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var request = new UploadInitiateRequest("test.txt", "text/plain");
        var initRes = await client.PostAsJsonAsync($"/api/modules/{module.Id}/documents/initiate", request);
        var initData = await initRes.Content.ReadFromJsonAsync<UploadInitiateResponse>();

        // Act
        var response = await client.DeleteAsync($"/api/documents/{initData!.DocumentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        _blobStorageMock.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Once);
        
        // Verify list is empty
        var listRes = await client.GetAsync($"/api/modules/{module.Id}/documents");
        var list = await listRes.Content.ReadFromJsonAsync<List<DocumentDto>>();
        list.Should().BeEmpty();
    }
}
