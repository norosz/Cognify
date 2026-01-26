using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
    public async Task UploadDocument_ShouldReturnCreated_WhenFileIsValid()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var module = await CreateModuleAsync(client);

        _blobStorageMock
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("blobs/guid_test.txt");

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "test.txt");

        // Act
        var response = await client.PostAsync($"/api/modules/{module.Id}/documents", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var doc = await response.Content.ReadFromJsonAsync<DocumentDto>();
        doc!.FileName.Should().Contain("test.txt");
        doc.ModuleId.Should().Be(module.Id);
    }

    [Fact]
    public async Task UploadDocument_ShouldReturnBadRequest_WhenFileIsMissing()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var module = await CreateModuleAsync(client);
        using var content = new MultipartFormDataContent();

        var response = await client.PostAsync($"/api/modules/{module.Id}/documents", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadDocument_ShouldReturnForbidden_WhenNotOwner()
    {
        var (ownerClient, _) = await CreateAuthenticatedClientAsync();
        var module = await CreateModuleAsync(ownerClient);

        var (otherClient, _) = await CreateAuthenticatedClientAsync();

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(new byte[] { 1 }), "file", "test.txt");

        var response = await otherClient.PostAsync($"/api/modules/{module.Id}/documents", content);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetModuleDocuments_ShouldReturnList_WhenDocumentsExist()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var module = await CreateModuleAsync(client);

        _blobStorageMock
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("path");

        // Upload one
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(new byte[] { 1 }), "file", "test.txt");
        await client.PostAsync($"/api/modules/{module.Id}/documents", content);

        // Act
        var response = await client.GetAsync($"/api/modules/{module.Id}/documents");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<DocumentDto>>();
        list.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteDocument_ShouldDelete_WhenOwner()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var module = await CreateModuleAsync(client);

        _blobStorageMock
             .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
             .ReturnsAsync("path");

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(new byte[] { 1 }), "file", "test.txt");
        var upRes = await client.PostAsync($"/api/modules/{module.Id}/documents", content);
        var doc = await upRes.Content.ReadFromJsonAsync<DocumentDto>();

        // Act
        var response = await client.DeleteAsync($"/api/documents/{doc!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        _blobStorageMock.Verify(x => x.DeleteAsync("path"), Times.Once);
        
        // Verify list is empty
        var listRes = await client.GetAsync($"/api/modules/{module.Id}/documents");
        var list = await listRes.Content.ReadFromJsonAsync<List<DocumentDto>>();
        list.Should().BeEmpty();
    }
}
