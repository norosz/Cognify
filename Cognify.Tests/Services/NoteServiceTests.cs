using Cognify.Server.Data;
using Cognify.Server.Dtos.Notes;
using Cognify.Server.Models;
using Cognify.Server.Services;
using Cognify.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cognify.Tests.Services;

public class NoteServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IUserContextService> _userContextMock;
    private readonly NoteService _service;
    private readonly Guid _userId;

    public NoteServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _userContextMock = new Mock<IUserContextService>();
        _userId = Guid.NewGuid();

        _userContextMock.Setup(x => x.GetCurrentUserId()).Returns(_userId);

        _service = new NoteService(_context, _userContextMock.Object);
    }

    private async Task<(Guid ModuleId, Guid NoteId)> SeedDataAsync(bool sameUser = true)
    {
        var ownerId = sameUser ? _userId : Guid.NewGuid();
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = ownerId, Title = "Test Module", Description = "Desc" };
        var note = new Note { Id = Guid.NewGuid(), ModuleId = module.Id, Title = "Test Note", Content = "Content" };

        _context.Modules.Add(module);
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        return (module.Id, note.Id);
    }

    [Fact]
    public async Task GetByModuleIdAsync_ShouldReturnNotes_WhenUserOwnsModule()
    {
        var (moduleId, _) = await SeedDataAsync(sameUser: true);

        var result = await _service.GetByModuleIdAsync(moduleId);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Test Note");
    }

    [Fact]
    public async Task GetByModuleIdAsync_ShouldReturnEmpty_WhenUserDoesNotOwnModule()
    {
        var (moduleId, _) = await SeedDataAsync(sameUser: false);

        var result = await _service.GetByModuleIdAsync(moduleId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateNote_WhenUserOwnsModule()
    {
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = _userId, Title = "My Module", Description = "D" };
        _context.Modules.Add(module);
        await _context.SaveChangesAsync();

        var dto = new CreateNoteDto { ModuleId = module.Id, Title = "New Note", Content = "New Content" };

        var result = await _service.CreateAsync(dto);

        result.Should().NotBeNull();
        result.Title.Should().Be("New Note");

        var dbNote = await _context.Notes.FindAsync(result.Id);
        dbNote.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenUserDoesNotOwnModule()
    {
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = Guid.NewGuid(), Title = "Other Module", Description = "D" };
        _context.Modules.Add(module);
        await _context.SaveChangesAsync();

        var dto = new CreateNoteDto { ModuleId = module.Id, Title = "New Note", Content = "New Content" };

        Func<Task> act = async () => await _service.CreateAsync(dto);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdate_WhenUserOwns()
    {
        var (moduleId, noteId) = await SeedDataAsync(sameUser: true);
        var dto = new UpdateNoteDto { Title = "Updated Title", Content = "Updated Content" };

        var result = await _service.UpdateAsync(noteId, dto);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated Title");

        var dbNote = await _context.Notes.FindAsync(noteId);
        dbNote!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task DeleteAsync_ShouldDelete_WhenUserOwns()
    {
        var (moduleId, noteId) = await SeedDataAsync(sameUser: true);

        var result = await _service.DeleteAsync(noteId);

        result.Should().BeTrue();
        var dbNote = await _context.Notes.FindAsync(noteId);
        dbNote.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
