using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cognify.Server;
using Cognify.Server.Data;
using Cognify.Server.Dtos.Auth;
using Cognify.Server.Dtos.Modules;
using Cognify.Server.Dtos.Notes;
using Cognify.Tests.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Cognify.Tests.Controllers;

public class NotesControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public NotesControllerTests(WebApplicationFactory<Program> factory)
    {
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
        var dto = new CreateModuleDto { Title = "Test Module", Description = "Desc" };
        var response = await client.PostAsJsonAsync("/api/modules", dto);
        return (await response.Content.ReadFromJsonAsync<ModuleDto>())!;
    }

    [Fact]
    public async Task CreateNote_ShouldReturnCreated_WhenModuleExistsAndOwned()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var module = await CreateModuleAsync(client);

        var dto = new CreateNoteDto 
        { 
            ModuleId = module.Id, 
            Title = "My Note", 
            Content = "Some content" 
        };

        var response = await client.PostAsJsonAsync("/api/notes", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<NoteDto>();
        created!.Title.Should().Be(dto.Title);
        created.Content.Should().Be(dto.Content);
        created.ModuleId.Should().Be(module.Id);
    }

    [Fact]
    public async Task CreateNote_ShouldReturnForbidden_WhenModuleOwnedByOtherUser()
    {
        var (client1, _) = await CreateAuthenticatedClientAsync(); // Owner
        var (client2, _) = await CreateAuthenticatedClientAsync(); // Attacker
        var module = await CreateModuleAsync(client1);

        var dto = new CreateNoteDto 
        { 
            ModuleId = module.Id, 
            Title = "Hacked Note" 
        };

        var response = await client2.PostAsJsonAsync("/api/notes", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetNotes_ShouldReturnList_WhenNotesExist()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var module = await CreateModuleAsync(client);

        await client.PostAsJsonAsync("/api/notes", new CreateNoteDto { ModuleId = module.Id, Title = "Note 1" });
        await client.PostAsJsonAsync("/api/notes", new CreateNoteDto { ModuleId = module.Id, Title = "Note 2" });

        var response = await client.GetAsync($"/api/notes/module/{module.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<NoteDto>>();
        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateNote_ShouldUpdate_WhenOwned()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var module = await CreateModuleAsync(client);
        
        var createRes = await client.PostAsJsonAsync("/api/notes", new CreateNoteDto { ModuleId = module.Id, Title = "Old Title" });
        var created = await createRes.Content.ReadFromJsonAsync<NoteDto>();

        var updateDto = new UpdateNoteDto { Title = "New Title", Content = "Updated Content" };
        var response = await client.PutAsJsonAsync($"/api/notes/{created!.Id}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<NoteDto>();
        updated!.Title.Should().Be("New Title");
        updated.Content.Should().Be("Updated Content");
    }

    [Fact]
    public async Task DeleteNote_ShouldDelete_WhenOwned()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var module = await CreateModuleAsync(client);
        
        var createRes = await client.PostAsJsonAsync("/api/notes", new CreateNoteDto { ModuleId = module.Id, Title = "To Delete" });
        var created = await createRes.Content.ReadFromJsonAsync<NoteDto>();

        var response = await client.DeleteAsync($"/api/notes/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getRes = await client.GetAsync($"/api/notes/{created.Id}");
        getRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
