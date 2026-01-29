using Cognify.Server.Data;
using Cognify.Server.Dtos.Modules;
using Cognify.Server.Models;
using Cognify.Server.Services;
using Cognify.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cognify.Tests.Services;

public class ModuleServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IUserContextService> _userContextMock;
    private readonly ModuleService _service;
    private readonly Guid _userId;

    public ModuleServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _userContextMock = new Mock<IUserContextService>();
        _userId = Guid.NewGuid();
        _userContextMock.Setup(x => x.GetCurrentUserId()).Returns(_userId);

        _service = new ModuleService(_context, _userContextMock.Object);
    }

    [Fact]
    public async Task GetModulesAsync_ShouldReturnCounts()
    {
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = _userId, Title = "Module" };
        var note = new Note { Id = Guid.NewGuid(), ModuleId = module.Id, Title = "Note" };
        var questionSet = new QuestionSet { Id = Guid.NewGuid(), NoteId = note.Id, Title = "Quiz" };
        var document = new Document { Id = Guid.NewGuid(), ModuleId = module.Id, FileName = "doc.pdf", BlobPath = "blob" };

        _context.Modules.Add(module);
        _context.Notes.Add(note);
        _context.QuestionSets.Add(questionSet);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        var results = await _service.GetModulesAsync();

        results.Should().HaveCount(1);
        results[0].DocumentsCount.Should().Be(1);
        results[0].NotesCount.Should().Be(1);
        results[0].QuizzesCount.Should().Be(1);
    }

    [Fact]
    public async Task GetModuleAsync_ShouldReturnNull_WhenNotOwned()
    {
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = Guid.NewGuid(), Title = "Module" };
        _context.Modules.Add(module);
        await _context.SaveChangesAsync();

        var result = await _service.GetModuleAsync(module.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetModuleAsync_ShouldReturnModule_WhenOwned()
    {
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = _userId, Title = "Module" };
        var note = new Note { Id = Guid.NewGuid(), ModuleId = module.Id, Title = "Note" };
        var questionSet = new QuestionSet { Id = Guid.NewGuid(), NoteId = note.Id, Title = "Quiz" };
        var document = new Document { Id = Guid.NewGuid(), ModuleId = module.Id, FileName = "doc.pdf", BlobPath = "blob" };

        _context.Modules.Add(module);
        _context.Notes.Add(note);
        _context.QuestionSets.Add(questionSet);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        var result = await _service.GetModuleAsync(module.Id);

        result.Should().NotBeNull();
        result!.DocumentsCount.Should().Be(1);
        result.NotesCount.Should().Be(1);
        result.QuizzesCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateModuleAsync_ShouldCreateModule()
    {
        var dto = new CreateModuleDto { Title = "New Module", Description = "Desc" };

        var result = await _service.CreateModuleAsync(dto);

        result.Title.Should().Be("New Module");
        var saved = await _context.Modules.FindAsync(result.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateModuleAsync_ShouldReturnNull_WhenNotOwned()
    {
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = Guid.NewGuid(), Title = "Module" };
        _context.Modules.Add(module);
        await _context.SaveChangesAsync();

        var result = await _service.UpdateModuleAsync(module.Id, new UpdateModuleDto { Title = "Updated", Description = "Desc" });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateModuleAsync_ShouldUpdate_WhenOwned()
    {
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = _userId, Title = "Module" };
        _context.Modules.Add(module);
        await _context.SaveChangesAsync();

        var result = await _service.UpdateModuleAsync(module.Id, new UpdateModuleDto { Title = "Updated", Description = "Desc" });

        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated");
    }

    [Fact]
    public async Task DeleteModuleAsync_ShouldReturnFalse_WhenNotOwned()
    {
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = Guid.NewGuid(), Title = "Module" };
        _context.Modules.Add(module);
        await _context.SaveChangesAsync();

        var result = await _service.DeleteModuleAsync(module.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteModuleAsync_ShouldDelete_WhenOwned()
    {
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = _userId, Title = "Module" };
        _context.Modules.Add(module);
        await _context.SaveChangesAsync();

        var result = await _service.DeleteModuleAsync(module.Id);

        result.Should().BeTrue();
        (await _context.Modules.FindAsync(module.Id)).Should().BeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
