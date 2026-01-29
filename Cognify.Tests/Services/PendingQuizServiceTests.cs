using Cognify.Server.Data;
using Cognify.Server.Models;
using Cognify.Server.Models.Ai;
using Cognify.Server.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace Cognify.Tests.Services;

public class PendingQuizServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PendingQuizService _service;
    private readonly Guid _userId;

    public PendingQuizServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _userId = Guid.NewGuid();

        var agentRunService = new AgentRunService(_context);
        _service = new PendingQuizService(_context, agentRunService);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreatePendingQuiz_WithGeneratingStatus()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        
        // Act
        var result = await _service.CreateAsync(_userId, noteId, moduleId, "Test Quiz", QuizDifficulty.Intermediate, 1, 5);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PendingQuizStatus.Generating);
        result.UserId.Should().Be(_userId);
        result.Title.Should().Be("Test Quiz");
        
        var dbRecord = await _context.PendingQuizzes.FindAsync(result.Id);
        dbRecord.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveAsQuizAsync_ShouldCreateQuestionSet_And_DeletePendingQuiz()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var pendingId = Guid.NewGuid();

        var questions = new List<GeneratedQuestion>
        {
            new GeneratedQuestion { Text = "Q1", Type = QuestionType.MultipleChoice, Options = ["A", "B"], CorrectAnswer = "A" }
        };
        var questionsJson = JsonSerializer.Serialize(questions);

        var pendingQuiz = new PendingQuiz
        {
            Id = pendingId,
            UserId = _userId,
            NoteId = noteId,
            ModuleId = moduleId,
            Title = "Saved Quiz",
            Status = PendingQuizStatus.Ready,
            QuestionsJson = questionsJson,
            QuestionType = (int)QuestionType.MultipleChoice,
            QuestionCount = 1,
            Difficulty = QuizDifficulty.Beginner
        };

        _context.PendingQuizzes.Add(pendingQuiz);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SaveAsQuizAsync(pendingId, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Saved Quiz");
        result.NoteId.Should().Be(noteId);

        var dbQs = await _context.QuestionSets.Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == result.Id);
        dbQs.Should().NotBeNull();
        dbQs!.Type.Should().Be(QuestionType.MultipleChoice);
        dbQs!.Questions.Should().HaveCount(1);
        dbQs.Questions.First().Prompt.Should().Be("Q1");

        var deletedPending = await _context.PendingQuizzes.FindAsync(pendingId);
        deletedPending.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsQuizAsync_ShouldThrow_WhenNotReady()
    {
        // Arrange
        var pendingId = Guid.NewGuid();
        var pendingQuiz = new PendingQuiz
        {
            Id = pendingId,
            UserId = _userId,
            NoteId = Guid.NewGuid(),
            ModuleId = Guid.NewGuid(),
            Title = "Not Ready Quiz",
            Status = PendingQuizStatus.Generating
        };

        _context.PendingQuizzes.Add(pendingQuiz);
        await _context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _service.SaveAsQuizAsync(pendingId, _userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Quiz is not ready to be saved.");
    }

    [Fact]
    public async Task SaveAsQuizAsync_ShouldThrow_WhenUnauthorized()
    {
        // Arrange
        var pendingId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var pendingQuiz = new PendingQuiz
        {
            Id = pendingId,
            UserId = otherUserId,
            NoteId = Guid.NewGuid(),
            ModuleId = Guid.NewGuid(),
            Title = "Other User Quiz",
            Status = PendingQuizStatus.Ready,
            QuestionsJson = "[]"
        };

        _context.PendingQuizzes.Add(pendingQuiz);
        await _context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _service.SaveAsQuizAsync(pendingId, _userId);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
