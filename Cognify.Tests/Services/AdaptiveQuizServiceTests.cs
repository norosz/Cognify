using Cognify.Server.Data;
using Cognify.Server.Dtos.Adaptive;
using Cognify.Server.Dtos.Knowledge;
using Cognify.Server.Models;
using Cognify.Server.Services;
using Cognify.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cognify.Tests.Services;

public class AdaptiveQuizServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IUserContextService> _userContextMock;
    private readonly Mock<IKnowledgeStateService> _knowledgeStateMock;
    private readonly Mock<IPendingQuizService> _pendingQuizMock;
    private readonly AdaptiveQuizService _service;
    private readonly Guid _userId;

    public AdaptiveQuizServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _userContextMock = new Mock<IUserContextService>();
        _knowledgeStateMock = new Mock<IKnowledgeStateService>();
        _pendingQuizMock = new Mock<IPendingQuizService>();
        _userId = Guid.NewGuid();

        _userContextMock.Setup(u => u.GetCurrentUserId()).Returns(_userId);

        _service = new AdaptiveQuizService(
            _context,
            _userContextMock.Object,
            _knowledgeStateMock.Object,
            _pendingQuizMock.Object);
    }

    [Fact]
    public async Task CreateAdaptiveQuizAsync_ReviewMode_UsesDueTopicAndMapsDifficulty()
    {
        var module = new Module { Id = Guid.NewGuid(), Title = "Biology", OwnerUserId = _userId };
        var note = new Note { Id = Guid.NewGuid(), ModuleId = module.Id, Title = "Cell Structure", Module = module };
        _context.Modules.Add(module);
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        _knowledgeStateMock.Setup(k => k.GetReviewQueueAsync(It.IsAny<int>()))
            .ReturnsAsync([
                new ReviewQueueItemDto
                {
                    Topic = "Biology / Cell Structure",
                    SourceNoteId = note.Id,
                    MasteryScore = 0.82,
                    ForgettingRisk = 0.2,
                    NextReviewAt = DateTime.UtcNow.AddDays(-1)
                }
            ]);

        _pendingQuizMock
            .Setup(p => p.CreateAsync(_userId, note.Id, module.Id, It.IsAny<string>(), It.IsAny<QuizDifficulty>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((Guid userId, Guid noteId, Guid moduleId, string title, QuizDifficulty difficulty, int type, int count) => new PendingQuiz
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                NoteId = noteId,
                ModuleId = moduleId,
                Title = title,
                Difficulty = difficulty,
                QuestionType = type,
                QuestionCount = count,
                Status = PendingQuizStatus.Generating,
                CreatedAt = DateTime.UtcNow
            });

        var request = new AdaptiveQuizRequest
        {
            Mode = AdaptiveQuizMode.Review,
            QuestionCount = 5,
            QuestionType = "MultipleChoice"
        };

        var result = await _service.CreateAdaptiveQuizAsync(request);

        result.PendingQuiz.Difficulty.Should().Be(QuizDifficulty.Advanced.ToString());
        result.SelectedTopic.Should().Be("Biology / Cell Structure");
    }

    [Fact]
    public async Task CreateAdaptiveQuizAsync_ReviewMode_ThrowsWhenNoEligibleTopics()
    {
        _knowledgeStateMock.Setup(k => k.GetReviewQueueAsync(It.IsAny<int>()))
            .ReturnsAsync([]);

        var request = new AdaptiveQuizRequest { Mode = AdaptiveQuizMode.Review };

        Func<Task> act = async () => await _service.CreateAdaptiveQuizAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAdaptiveQuizAsync_NoteMode_UsesDefaultsWhenNoState()
    {
        var module = new Module { Id = Guid.NewGuid(), Title = "Physics", OwnerUserId = _userId };
        var note = new Note { Id = Guid.NewGuid(), ModuleId = module.Id, Title = "Kinematics", Module = module };
        _context.Modules.Add(module);
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        _knowledgeStateMock.Setup(k => k.GetMyStatesAsync())
            .ReturnsAsync([]);

        _pendingQuizMock
            .Setup(p => p.CreateAsync(_userId, note.Id, module.Id, It.IsAny<string>(), It.IsAny<QuizDifficulty>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((Guid userId, Guid noteId, Guid moduleId, string title, QuizDifficulty difficulty, int type, int count) => new PendingQuiz
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                NoteId = noteId,
                ModuleId = moduleId,
                Title = title,
                Difficulty = difficulty,
                QuestionType = type,
                QuestionCount = count,
                Status = PendingQuizStatus.Generating,
                CreatedAt = DateTime.UtcNow
            });

        var request = new AdaptiveQuizRequest
        {
            Mode = AdaptiveQuizMode.Note,
            NoteId = note.Id,
            QuestionCount = 5
        };

        var result = await _service.CreateAdaptiveQuizAsync(request);

        result.PendingQuiz.Difficulty.Should().Be(QuizDifficulty.Intermediate.ToString());
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
