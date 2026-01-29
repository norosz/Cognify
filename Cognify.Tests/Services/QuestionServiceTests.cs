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

public class QuizServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IUserContextService> _userContextMock;
    private readonly QuizService _quizService;
    private readonly Guid _userId;

    public QuizServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _userContextMock = new Mock<IUserContextService>();
        _userId = Guid.NewGuid();

        _userContextMock.Setup(uc => uc.GetCurrentUserId()).Returns(_userId);

        _quizService = new QuizService(_context, _userContextMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateQuiz_WhenUserOwnsNote()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = _userId, Title = "Test Module" };
        var note = new Note { Id = noteId, ModuleId = module.Id, Title = "Note", Content = "Content" };
        
        _context.Modules.Add(module);
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        var dto = new CreateQuizDto 
        { 
            NoteId = noteId,
            Questions = 
            [
                new QuizQuestionDto { Prompt = "Q1", Type = "MultipleChoice", Options = ["A", "B"], CorrectAnswer = "A", Explanation = "Exp" }
            ]
        };

        // Act
        var result = await _quizService.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be("MultipleChoice");
        result.Questions.Should().HaveCount(1);
        result.Questions[0].Prompt.Should().Be("Q1");
        
        var saved = await _context.Quizzes.Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == result.Id);
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

        var dto = new CreateQuizDto { NoteId = noteId };

        // Act
        Func<Task> act = async () => await _quizService.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetByNoteIdAsync_ShouldReturnQuizzes_WhenUserOwnsModule()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = _userId, Title = "Test Module" };
        var note = new Note { Id = noteId, ModuleId = module.Id, Title = "Note", Content = "Content" };
        
        _context.Modules.Add(module);
        _context.Notes.Add(note);
        
        var qs1 = new Quiz { NoteId = noteId, Title = "QS1", CreatedAt = DateTime.UtcNow.AddMinutes(-5) };
        var qs2 = new Quiz { NoteId = noteId, Title = "QS2", CreatedAt = DateTime.UtcNow };
        _context.Quizzes.AddRange(qs1, qs2);
        
        await _context.SaveChangesAsync();

        // Act
        var result = await _quizService.GetByNoteIdAsync(noteId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInDescendingOrder(q => q.CreatedAt);
    }

    [Fact]
    public async Task GetByNoteIdAsync_ShouldFilterOutNotOwned()
    {
        var noteId = Guid.NewGuid();
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = _userId, Title = "Test Module" };
        var note = new Note { Id = noteId, ModuleId = module.Id, Title = "Note", Content = "Content" };

        var otherModule = new Module { Id = Guid.NewGuid(), OwnerUserId = Guid.NewGuid(), Title = "Other" };
        var otherNote = new Note { Id = Guid.NewGuid(), ModuleId = otherModule.Id, Title = "Other Note" };

        _context.Modules.AddRange(module, otherModule);
        _context.Notes.AddRange(note, otherNote);

        var qs1 = new Quiz { NoteId = noteId, Title = "QS1" };
        var qs2 = new Quiz { NoteId = otherNote.Id, Title = "QS2" };
        _context.Quizzes.AddRange(qs1, qs2);
        await _context.SaveChangesAsync();

        var result = await _quizService.GetByNoteIdAsync(noteId);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("QS1");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotOwned()
    {
        var noteId = Guid.NewGuid();
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = Guid.NewGuid(), Title = "Other Module" };
        var note = new Note { Id = noteId, ModuleId = module.Id, Title = "Note", Content = "Content" };
        var qs = new Quiz { NoteId = noteId, Title = "QS" };

        _context.Modules.Add(module);
        _context.Notes.Add(note);
        _context.Quizzes.Add(qs);
        await _context.SaveChangesAsync();

        var result = await _quizService.GetByIdAsync(qs.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnQuiz_WhenOwned()
    {
        var noteId = Guid.NewGuid();
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = _userId, Title = "Test Module" };
        var note = new Note { Id = noteId, ModuleId = module.Id, Title = "Note", Content = "Content" };
        var qs = new Quiz { NoteId = noteId, Title = "QS" };

        _context.Modules.Add(module);
        _context.Notes.Add(note);
        _context.Quizzes.Add(qs);
        await _context.SaveChangesAsync();

        var result = await _quizService.GetByIdAsync(qs.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(qs.Id);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenUserOwnsNote()
    {
         // Arrange
        var noteId = Guid.NewGuid();
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = _userId, Title = "Test Module" };
        var note = new Note { Id = noteId, ModuleId = module.Id, Title = "Note", Content = "Content" };
        var qs = new Quiz { NoteId = noteId, Title = "QS" };
        
        _context.Modules.Add(module);
        _context.Notes.Add(note);
        _context.Quizzes.Add(qs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _quizService.DeleteAsync(qs.Id);

        // Assert
        result.Should().BeTrue();
        var deleted = await _context.Quizzes.FindAsync(qs.Id);
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
        var qs = new Quiz { NoteId = noteId, Title = "QS" };
        
        _context.Modules.Add(module);
        _context.Notes.Add(note);
        _context.Quizzes.Add(qs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _quizService.DeleteAsync(qs.Id);

        // Assert
        result.Should().BeFalse();
        var exists = await _context.Quizzes.FindAsync(qs.Id);
        exists.Should().NotBeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
