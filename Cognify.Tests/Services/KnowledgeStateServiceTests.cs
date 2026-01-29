using Cognify.Server.Data;
using Cognify.Server.Dtos.Knowledge;
using Cognify.Server.Models;
using Cognify.Server.Services;
using Cognify.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cognify.Tests.Services;

public class KnowledgeStateServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IUserContextService> _userContextMock;
    private readonly KnowledgeStateService _service;
    private readonly Guid _userId;

    public KnowledgeStateServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _userContextMock = new Mock<IUserContextService>();
        _userId = Guid.NewGuid();

        _userContextMock.Setup(uc => uc.GetCurrentUserId()).Returns(_userId);
        _service = new KnowledgeStateService(
            _context,
            _userContextMock.Object,
            new DecayPredictionService(),
            new MistakeAnalysisService());
    }

    [Fact]
    public async Task ApplyAttemptResultAsync_CreatesStateAndInteractions()
    {
        var module = new Module { Id = Guid.NewGuid(), Title = "Biology", OwnerUserId = _userId };
        var note = new Note { Id = Guid.NewGuid(), ModuleId = module.Id, Title = "Cell Structure", Module = module };
        var questionSet = new QuestionSet { Id = Guid.NewGuid(), NoteId = note.Id, Title = "Quiz", Note = note };
        var attempt = new Attempt { Id = Guid.NewGuid(), UserId = _userId, QuestionSetId = questionSet.Id, AnswersJson = "{}", Score = 50 };

        var interactions = new List<KnowledgeInteractionInput>
        {
            new() { QuestionId = Guid.NewGuid(), UserAnswer = "A", IsCorrect = true },
            new() { QuestionId = Guid.NewGuid(), UserAnswer = "B", IsCorrect = false }
        };

        await _service.ApplyAttemptResultAsync(attempt, questionSet, interactions);

        var state = await _context.UserKnowledgeStates.FirstOrDefaultAsync(s => s.UserId == _userId);
        state.Should().NotBeNull();
        state!.Topic.Should().Be("Biology / Cell Structure");
        state.NextReviewAt.Should().NotBeNull();

        var savedInteractions = await _context.LearningInteractions.Where(i => i.UserId == _userId).ToListAsync();
        savedInteractions.Should().HaveCount(2);

        var evaluations = await _context.AnswerEvaluations.ToListAsync();
        evaluations.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetReviewQueueAsync_ReturnsDueItemsOnly()
    {
        _context.UserKnowledgeStates.AddRange(
            new UserKnowledgeState
            {
                UserId = _userId,
                Topic = "Topic A",
                MasteryScore = 0.4,
                ConfidenceScore = 0.4,
                ForgettingRisk = 0.6,
                NextReviewAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            },
            new UserKnowledgeState
            {
                UserId = _userId,
                Topic = "Topic B",
                MasteryScore = 0.9,
                ConfidenceScore = 0.9,
                ForgettingRisk = 0.1,
                NextReviewAt = DateTime.UtcNow.AddDays(5),
                UpdatedAt = DateTime.UtcNow
            });

        await _context.SaveChangesAsync();

        var queue = await _service.GetReviewQueueAsync();

        queue.Should().HaveCount(1);
        queue[0].Topic.Should().Be("Topic A");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
