using Cognify.Server.Data;
using Cognify.Server.DTOs;
using Cognify.Server.Models;
using Cognify.Server.Services;
using Cognify.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cognify.Tests.Services;

public class QuestionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IUserContextService> _userContextMock;
    private readonly QuestionService _questionService;
    private readonly Guid _userId;

    public QuestionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _userContextMock = new Mock<IUserContextService>();
        _userId = Guid.NewGuid();

        _userContextMock.Setup(uc => uc.GetCurrentUserId()).Returns(_userId);

        _questionService = new QuestionService(_context, _userContextMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateQuestionSet_WhenUserOwnsNote()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = _userId, Title = "Test Module" };
        var note = new Note { Id = noteId, ModuleId = module.Id, Title = "Note", Content = "Content" };
        
        _context.Modules.Add(module);
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        var dto = new CreateQuestionSetDto 
        { 
            NoteId = noteId,
            Questions = 
            [
                new QuestionDto { Prompt = "Q1", Type = "MultipleChoice", Options = ["A", "B"], CorrectAnswer = "A", Explanation = "Exp" }
            ]
        };

        // Act
        var result = await _questionService.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be("MultipleChoice");
        result.Questions.Should().HaveCount(1);
        result.Questions[0].Prompt.Should().Be("Q1");
        
        var saved = await _context.QuestionSets.Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == result.Id);
        saved.Should().NotBeNull();
        saved!.Type.Should().Be(QuestionType.MultipleChoice);
        saved!.Questions.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowUnauthorized_WhenUserDoesNotOwnNote()
    {
         // Arrange
        var otherUserId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = otherUserId, Title = "Other Module" };
        var note = new Note { Id = noteId, ModuleId = module.Id, Title = "Note", Content = "Content" };
        
        _context.Modules.Add(module);
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        var dto = new CreateQuestionSetDto { NoteId = noteId };

        // Act
        Func<Task> act = async () => await _questionService.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetByNoteIdAsync_ShouldReturnQuestionSets_WhenUserOwnsModule()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = _userId, Title = "Test Module" };
        var note = new Note { Id = noteId, ModuleId = module.Id, Title = "Note", Content = "Content" };
        
        _context.Modules.Add(module);
        _context.Notes.Add(note);
        
        var qs1 = new QuestionSet { NoteId = noteId, Title = "QS1", CreatedAt = DateTime.UtcNow.AddMinutes(-5) };
        var qs2 = new QuestionSet { NoteId = noteId, Title = "QS2", CreatedAt = DateTime.UtcNow };
        _context.QuestionSets.AddRange(qs1, qs2);
        
        await _context.SaveChangesAsync();

        // Act
        var result = await _questionService.GetByNoteIdAsync(noteId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInDescendingOrder(q => q.CreatedAt);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenUserOwnsNote()
    {
         // Arrange
        var noteId = Guid.NewGuid();
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = _userId, Title = "Test Module" };
        var note = new Note { Id = noteId, ModuleId = module.Id, Title = "Note", Content = "Content" };
        var qs = new QuestionSet { NoteId = noteId, Title = "QS" };
        
        _context.Modules.Add(module);
        _context.Notes.Add(note);
        _context.QuestionSets.Add(qs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _questionService.DeleteAsync(qs.Id);

        // Assert
        result.Should().BeTrue();
        var deleted = await _context.QuestionSets.FindAsync(qs.Id);
        deleted.Should().BeNull();
    }
    
    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenUserDoesNotOwnNote()
    {
         // Arrange
        var otherUserId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = otherUserId, Title = "Other Module" };
        var note = new Note { Id = noteId, ModuleId = module.Id, Title = "Note", Content = "Content" };
        var qs = new QuestionSet { NoteId = noteId, Title = "QS" };
        
        _context.Modules.Add(module);
        _context.Notes.Add(note);
        _context.QuestionSets.Add(qs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _questionService.DeleteAsync(qs.Id);

        // Assert
        result.Should().BeFalse();
        var exists = await _context.QuestionSets.FindAsync(qs.Id);
        exists.Should().NotBeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
