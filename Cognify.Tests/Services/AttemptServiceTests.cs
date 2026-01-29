using Cognify.Server.Data;
using Cognify.Server.DTOs;
using Cognify.Server.Dtos.Knowledge;
using Cognify.Server.Dtos.Ai.Contracts;
using Cognify.Server.Models;
using Cognify.Server.Services;
using Cognify.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text.Json;
using Xunit;

namespace Cognify.Tests.Services;

public class AttemptServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IUserContextService> _userContextMock;
    private readonly Mock<IKnowledgeStateService> _knowledgeStateMock;
    private readonly Mock<IAiService> _aiServiceMock;
    private readonly Mock<IAgentRunService> _agentRunServiceMock;
    private readonly AttemptService _attemptService;
    private readonly Guid _userId;

    public AttemptServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _userContextMock = new Mock<IUserContextService>();
        _knowledgeStateMock = new Mock<IKnowledgeStateService>();
        _aiServiceMock = new Mock<IAiService>();
        _agentRunServiceMock = new Mock<IAgentRunService>();
        _userId = Guid.NewGuid();

        _userContextMock.Setup(uc => uc.GetCurrentUserId()).Returns(_userId);
        _knowledgeStateMock
            .Setup(ks => ks.ApplyAttemptResultAsync(It.IsAny<Attempt>(), It.IsAny<QuestionSet>(), It.IsAny<IReadOnlyCollection<KnowledgeInteractionInput>>()))
            .Returns(Task.CompletedTask);

        _attemptService = new AttemptService(
            _context,
            _userContextMock.Object,
            _knowledgeStateMock.Object,
            _aiServiceMock.Object,
            _agentRunServiceMock.Object);
    }

    [Fact]
    public async Task SubmitAttemptAsync_ShouldCalculateScoreCorrectly()
    {
        // Arrange
        var module = new Module { Id = Guid.NewGuid(), Title = "Module", OwnerUserId = _userId };
        var noteId = Guid.NewGuid();
        var note = new Note { Id = noteId, ModuleId = module.Id, Title = "Note", Module = module };
        var qsId = Guid.NewGuid();
        var question1 = new Question 
        { 
            Id = Guid.NewGuid(), 
            QuestionSetId = qsId,
            Prompt = "Q1",
            Type = QuestionType.MultipleChoice,
            OptionsJson = "[\"A\",\"B\"]",
            CorrectAnswerJson = "\"A\"",
            Explanation = "Exp"
        };
        var question2 = new Question 
        { 
            Id = Guid.NewGuid(), 
            QuestionSetId = qsId,
            Prompt = "Q2",
            Type = QuestionType.MultipleChoice,
            OptionsJson = "[\"C\",\"D\"]",
            CorrectAnswerJson = "\"C\"", // User will answer D (wrong)
            Explanation = "Exp"
        };

        var questionSet = new QuestionSet 
        { 
            Id = qsId, 
            NoteId = noteId,
            Title = "Test Quiz",
            Note = note,
            Questions = [question1, question2]
        };

        _context.Modules.Add(module);
        _context.Notes.Add(note);
        _context.QuestionSets.Add(questionSet);
        await _context.SaveChangesAsync();

        var dto = new SubmitAttemptDto
        {
            QuestionSetId = qsId,
            Answers = new Dictionary<string, string>
            {
                { question1.Id.ToString(), "A" }, // Correct
                { question2.Id.ToString(), "D" }  // Incorrect
            }
        };

        // Act
        var result = await _attemptService.SubmitAttemptAsync(dto);

        // Assert
        result.Score.Should().Be(50); // 1 out of 2 correct
        result.Answers.Should().HaveCount(2);
        
        var saved = await _context.Attempts.FirstOrDefaultAsync(a => a.Id == result.Id);
        saved.Should().NotBeNull();
        saved!.Score.Should().Be(50);
        saved.UserId.Should().Be(_userId);

        _knowledgeStateMock.Verify(ks => ks.ApplyAttemptResultAsync(
            It.IsAny<Attempt>(),
            It.IsAny<QuestionSet>(),
            It.IsAny<IReadOnlyCollection<KnowledgeInteractionInput>>()), Times.Once);
    }

    [Fact]
    public async Task GetAttemptsAsync_ShouldReturnUsersAttempts_OrderedByDate()
    {
         // Arrange
        var qsId = Guid.NewGuid();
        var attempt1 = new Attempt { Id = Guid.NewGuid(), QuestionSetId = qsId, UserId = _userId, CreatedAt = DateTime.UtcNow.AddMinutes(-5), AnswersJson = "{}", Score = 100 };
        var attempt2 = new Attempt { Id = Guid.NewGuid(), QuestionSetId = qsId, UserId = _userId, CreatedAt = DateTime.UtcNow, AnswersJson = "{}", Score = 0 };
        var otherAttempt = new Attempt { Id = Guid.NewGuid(), QuestionSetId = qsId, UserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, AnswersJson = "{}", Score = 0 };

        _context.Attempts.AddRange(attempt1, attempt2, otherAttempt);
        await _context.SaveChangesAsync();

        // Act
        var result = await _attemptService.GetAttemptsAsync(qsId);

        // Assert
        result.Should().HaveCount(2);
        result.First().Id.Should().Be(attempt2.Id); // Newer first
        result.Should().NotContain(a => a.Id == otherAttempt.Id);
    }
    
    [Fact]
    public async Task SubmitAttemptAsync_ShouldHandleCaseInsensitiveKeys()
    {
         // Arrange
        var module = new Module { Id = Guid.NewGuid(), Title = "Module", OwnerUserId = _userId };
        var noteId = Guid.NewGuid();
        var note = new Note { Id = noteId, ModuleId = module.Id, Title = "Note", Module = module };
        var qsId = Guid.NewGuid();
        var question = new Question 
        { 
            Id = Guid.NewGuid(), 
            QuestionSetId = qsId,
            Prompt = "Q1",
            Type = QuestionType.MultipleChoice,
            OptionsJson = "[\"A\",\"B\"]",
            CorrectAnswerJson = "\"A\"",
            Explanation = ""
        };

        var questionSet = new QuestionSet { Id = qsId, NoteId = noteId, Title = "Test Quiz", Note = note, Questions = [question] };
        _context.Modules.Add(module);
        _context.Notes.Add(note);
        _context.QuestionSets.Add(questionSet);
        await _context.SaveChangesAsync();

        var dto = new SubmitAttemptDto
        {
            QuestionSetId = qsId,
            Answers = new Dictionary<string, string>
            {
                { question.Id.ToString().ToUpper(), "A" } 
            }
        };

        // Act
        var result = await _attemptService.SubmitAttemptAsync(dto);

        // Assert
        result.Score.Should().Be(100);
    }

    [Fact]
    public async Task GetAttemptByIdAsync_ShouldReturnAttempt_WhenOwned()
    {
        var attempt = new Attempt
        {
            Id = Guid.NewGuid(),
            QuestionSetId = Guid.NewGuid(),
            UserId = _userId,
            AnswersJson = "{}",
            Score = 75
        };

        _context.Attempts.Add(attempt);
        await _context.SaveChangesAsync();

        var result = await _attemptService.GetAttemptByIdAsync(attempt.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(attempt.Id);
    }

    [Fact]
    public async Task GetAttemptByIdAsync_ShouldReturnNull_WhenNotOwned()
    {
        var attempt = new Attempt
        {
            Id = Guid.NewGuid(),
            QuestionSetId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            AnswersJson = "{}",
            Score = 75
        };

        _context.Attempts.Add(attempt);
        await _context.SaveChangesAsync();

        var result = await _attemptService.GetAttemptByIdAsync(attempt.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SubmitAttemptAsync_ShouldGradeOpenText_WithAiFeedback()
    {
        var module = new Module { Id = Guid.NewGuid(), Title = "Module", OwnerUserId = _userId };
        var noteId = Guid.NewGuid();
        var note = new Note { Id = noteId, ModuleId = module.Id, Title = "Note", Module = module };
        var qsId = Guid.NewGuid();
        var question = new Question
        {
            Id = Guid.NewGuid(),
            QuestionSetId = qsId,
            Prompt = "Explain concept",
            Type = QuestionType.OpenText,
            OptionsJson = "[]",
            CorrectAnswerJson = "\"Expected\"",
            Explanation = "Details"
        };

        var questionSet = new QuestionSet
        {
            Id = qsId,
            NoteId = noteId,
            Title = "Open Text Quiz",
            Note = note,
            Questions = [question]
        };

        _context.Modules.Add(module);
        _context.Notes.Add(note);
        _context.QuestionSets.Add(questionSet);
        await _context.SaveChangesAsync();

        _agentRunServiceMock
            .Setup(s => s.CreateAsync(_userId, AgentRunType.Grading, It.IsAny<string>(), question.Id.ToString(), "grading-v2"))
            .ReturnsAsync(new AgentRun { Id = Guid.NewGuid(), UserId = _userId, Type = AgentRunType.Grading, Status = AgentRunStatus.Pending });

        _aiServiceMock
            .Setup(s => s.GradeAnswerAsync(It.IsAny<GradingContractRequest>()))
            .ReturnsAsync(new GradingContractResponse(
                AgentContractVersions.V2,
                Score: 80,
                MaxScore: 100,
                Feedback: "Solid answer",
                DetectedMistakes: null,
                ConfidenceEstimate: null,
                RawAnalysis: "Score: 80\nFeedback: Solid answer"));

        var dto = new SubmitAttemptDto
        {
            QuestionSetId = qsId,
            Answers = new Dictionary<string, string>
            {
                { question.Id.ToString(), "My answer" }
            }
        };

        var result = await _attemptService.SubmitAttemptAsync(dto);

        result.Score.Should().Be(80);
        _agentRunServiceMock.Verify(s => s.MarkCompletedAsync(It.IsAny<Guid>(), It.IsAny<string>(), null, null, null), Times.Once);
    }

    [Fact]
    public async Task SubmitAttemptAsync_ShouldHandleMultipleQuestionTypes()
    {
        var module = new Module { Id = Guid.NewGuid(), Title = "Module", OwnerUserId = _userId };
        var noteId = Guid.NewGuid();
        var note = new Note { Id = noteId, ModuleId = module.Id, Title = "Note", Module = module };
        var qsId = Guid.NewGuid();

        var ordering = new Question
        {
            Id = Guid.NewGuid(),
            QuestionSetId = qsId,
            Prompt = "Order",
            Type = QuestionType.Ordering,
            OptionsJson = "[]",
            CorrectAnswerJson = "\"A|B|C\""
        };

        var multiSelect = new Question
        {
            Id = Guid.NewGuid(),
            QuestionSetId = qsId,
            Prompt = "Select",
            Type = QuestionType.MultipleSelect,
            OptionsJson = "[]",
            CorrectAnswerJson = "\"A|C\""
        };

        var matching = new Question
        {
            Id = Guid.NewGuid(),
            QuestionSetId = qsId,
            Prompt = "Match",
            Type = QuestionType.Matching,
            OptionsJson = "[]",
            CorrectAnswerJson = "\"A:1|B:2\""
        };

        var trueFalse = new Question
        {
            Id = Guid.NewGuid(),
            QuestionSetId = qsId,
            Prompt = "True/False",
            Type = QuestionType.TrueFalse,
            OptionsJson = "[]",
            CorrectAnswerJson = "\"True\""
        };

        var questionSet = new QuestionSet
        {
            Id = qsId,
            NoteId = noteId,
            Title = "Mixed Quiz",
            Note = note,
            Questions = [ordering, multiSelect, matching, trueFalse]
        };

        _context.Modules.Add(module);
        _context.Notes.Add(note);
        _context.QuestionSets.Add(questionSet);
        await _context.SaveChangesAsync();

        var dto = new SubmitAttemptDto
        {
            QuestionSetId = qsId,
            Answers = new Dictionary<string, string>
            {
                { ordering.Id.ToString(), "A|B|C" },
                { multiSelect.Id.ToString(), "A|C" },
                { matching.Id.ToString(), "A:1|B:2" },
                { trueFalse.Id.ToString(), "True" }
            }
        };

        var result = await _attemptService.SubmitAttemptAsync(dto);

        result.Score.Should().Be(100);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
