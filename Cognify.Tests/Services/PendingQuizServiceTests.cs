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
    public async Task CreateAsync_ShouldResolveModuleId_FromNote_WhenEmpty()
    {
        var moduleId = Guid.NewGuid();
        var noteId = Guid.NewGuid();

        _context.Modules.Add(new Module { Id = moduleId, OwnerUserId = _userId, Title = "Module" });
        _context.Notes.Add(new Note { Id = noteId, ModuleId = moduleId, Title = "Note" });
        await _context.SaveChangesAsync();

        var result = await _service.CreateAsync(_userId, noteId, Guid.Empty, "Title", QuizDifficulty.Beginner, 0, 3);

        result.ModuleId.Should().Be(moduleId);
    }

    [Fact]
    public async Task GetByUserAsync_ShouldReturnQuizzes_WithNoteAndModule()
    {
        var moduleId = Guid.NewGuid();
        var noteId = Guid.NewGuid();

        _context.Modules.Add(new Module { Id = moduleId, OwnerUserId = _userId, Title = "Module" });
        _context.Notes.Add(new Note { Id = noteId, ModuleId = moduleId, Title = "Note" });

        _context.PendingQuizzes.Add(new PendingQuiz
        {
            UserId = _userId,
            NoteId = noteId,
            ModuleId = moduleId,
            Title = "Quiz",
            Status = PendingQuizStatus.Generating
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetByUserAsync(_userId);

        result.Should().HaveCount(1);
        result[0].Note.Should().NotBeNull();
        result[0].Module.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnQuiz_WhenFound()
    {
        var moduleId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var pending = new PendingQuiz
        {
            UserId = _userId,
            NoteId = noteId,
            ModuleId = moduleId,
            Title = "Quiz",
            Status = PendingQuizStatus.Generating
        };

        _context.Modules.Add(new Module { Id = moduleId, OwnerUserId = _userId, Title = "Module" });
        _context.Notes.Add(new Note { Id = noteId, ModuleId = moduleId, Title = "Note" });
        _context.PendingQuizzes.Add(pending);
        await _context.SaveChangesAsync();

        var result = await _service.GetByIdAsync(pending.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(pending.Id);
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
    public async Task SaveAsQuizAsync_ShouldPersistQuizRubric_WhenEnvelopeProvided()
    {
        var noteId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var pendingId = Guid.NewGuid();
        var rubric = "Score full credit when answers include the key steps and definitions.";

        var questions = new List<GeneratedQuestion>
        {
            new GeneratedQuestion { Text = "Q1", Type = QuestionType.MultipleChoice, Options = ["A", "B"], CorrectAnswer = "A" }
        };

        var options = new JsonSerializerOptions { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
        var envelope = new { questions, quizRubric = rubric };
        var questionsJson = JsonSerializer.Serialize(envelope, options);

        var pendingQuiz = new PendingQuiz
        {
            Id = pendingId,
            UserId = _userId,
            NoteId = noteId,
            ModuleId = moduleId,
            Title = "Rubric Quiz",
            Status = PendingQuizStatus.Ready,
            QuestionsJson = questionsJson,
            QuestionType = (int)QuestionType.MultipleChoice,
            QuestionCount = 1,
            Difficulty = QuizDifficulty.Beginner
        };

        _context.PendingQuizzes.Add(pendingQuiz);
        await _context.SaveChangesAsync();

        var result = await _service.SaveAsQuizAsync(pendingId, _userId);

        var dbQs = await _context.QuestionSets.FirstOrDefaultAsync(q => q.Id == result.Id);
        dbQs.Should().NotBeNull();
        dbQs!.RubricJson.Should().Be(rubric);
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
    public async Task SaveAsQuizAsync_ShouldThrow_WhenNoQuestionsJson()
    {
        var pendingId = Guid.NewGuid();
        var pendingQuiz = new PendingQuiz
        {
            Id = pendingId,
            UserId = _userId,
            NoteId = Guid.NewGuid(),
            ModuleId = Guid.NewGuid(),
            Title = "Empty Quiz",
            Status = PendingQuizStatus.Ready,
            QuestionsJson = null
        };

        _context.PendingQuizzes.Add(pendingQuiz);
        await _context.SaveChangesAsync();

        Func<Task> act = async () => await _service.SaveAsQuizAsync(pendingId, _userId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No questions generated.");
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

    [Fact]
    public async Task SaveAsQuizAsync_ShouldFlattenArrayCorrectAnswer()
    {
        var noteId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var pendingId = Guid.NewGuid();

        var questions = new List<GeneratedQuestion>
        {
            new GeneratedQuestion { Text = "Q1", Type = QuestionType.MultipleSelect, Options = ["A", "B"], CorrectAnswer = new[] { "A", "B" } }
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
            QuestionType = (int)QuestionType.MultipleSelect,
            QuestionCount = 1,
            Difficulty = QuizDifficulty.Beginner
        };

        _context.PendingQuizzes.Add(pendingQuiz);
        await _context.SaveChangesAsync();

        var result = await _service.SaveAsQuizAsync(pendingId, _userId);

        var dbQuestion = await _context.Questions.FirstOrDefaultAsync(q => q.QuestionSetId == result.Id);
        dbQuestion.Should().NotBeNull();
        dbQuestion!.CorrectAnswerJson.Should().Be("\"A|B\"");
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldUpdateFields()
    {
        var pending = new PendingQuiz
        {
            UserId = _userId,
            NoteId = Guid.NewGuid(),
            ModuleId = Guid.NewGuid(),
            Title = "Quiz",
            Status = PendingQuizStatus.Generating
        };
        _context.PendingQuizzes.Add(pending);
        await _context.SaveChangesAsync();

        await _service.UpdateStatusAsync(pending.Id, PendingQuizStatus.Ready, questionsJson: "[]", errorMessage: "", actualQuestionCount: 2);

        var updated = await _context.PendingQuizzes.FindAsync(pending.Id);
        updated!.Status.Should().Be(PendingQuizStatus.Ready);
        updated.QuestionsJson.Should().Be("[]");
        updated.ActualQuestionCount.Should().Be(2);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveQuiz()
    {
        var pending = new PendingQuiz
        {
            UserId = _userId,
            NoteId = Guid.NewGuid(),
            ModuleId = Guid.NewGuid(),
            Title = "Quiz",
            Status = PendingQuizStatus.Generating
        };
        _context.PendingQuizzes.Add(pending);
        await _context.SaveChangesAsync();

        await _service.DeleteAsync(pending.Id);

        var deleted = await _context.PendingQuizzes.FindAsync(pending.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task GetCountByUserAsync_ShouldReturnCount()
    {
        _context.PendingQuizzes.AddRange(
            new PendingQuiz { UserId = _userId, NoteId = Guid.NewGuid(), ModuleId = Guid.NewGuid(), Title = "Q1", Status = PendingQuizStatus.Generating },
            new PendingQuiz { UserId = Guid.NewGuid(), NoteId = Guid.NewGuid(), ModuleId = Guid.NewGuid(), Title = "Q2", Status = PendingQuizStatus.Generating }
        );
        await _context.SaveChangesAsync();

        var count = await _service.GetCountByUserAsync(_userId);

        count.Should().Be(1);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
