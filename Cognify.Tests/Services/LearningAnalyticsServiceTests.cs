using Cognify.Server.Data;
using Cognify.Server.Dtos.Analytics;
using Cognify.Server.Models;
using Cognify.Server.Services;
using Cognify.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cognify.Tests.Services;

public class LearningAnalyticsServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IUserContextService> _userContextMock;
    private readonly LearningAnalyticsService _service;
    private readonly Guid _userId;

    public LearningAnalyticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _userContextMock = new Mock<IUserContextService>();
        _userId = Guid.NewGuid();
        _userContextMock.Setup(u => u.GetCurrentUserId()).Returns(_userId);

        var computationService = new LearningAnalyticsComputationService(_context, new DecayPredictionService());
        _service = new LearningAnalyticsService(_userContextMock.Object, computationService);
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsAggregateMetrics()
    {
        _context.UserKnowledgeStates.AddRange(
            new UserKnowledgeState
            {
                UserId = _userId,
                Topic = "Math / Algebra",
                MasteryScore = 0.8,
                ConfidenceScore = 0.7,
                ForgettingRisk = 0.2,
                UpdatedAt = DateTime.UtcNow
            },
            new UserKnowledgeState
            {
                UserId = _userId,
                Topic = "Science / Biology",
                MasteryScore = 0.4,
                ConfidenceScore = 0.4,
                ForgettingRisk = 0.7,
                UpdatedAt = DateTime.UtcNow
            });

        _context.Attempts.AddRange(
            new Attempt { UserId = _userId, QuestionSetId = Guid.NewGuid(), AnswersJson = "{}", Score = 90, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Attempt { UserId = _userId, QuestionSetId = Guid.NewGuid(), AnswersJson = "{}", Score = 60, CreatedAt = DateTime.UtcNow }
        );

        await _context.SaveChangesAsync();

        var summary = await _service.GetSummaryAsync();

        summary.TotalTopics.Should().Be(2);
        summary.TotalAttempts.Should().Be(2);
        summary.AccuracyRate.Should().BeApproximately(0.75, 0.01);
        summary.AverageMastery.Should().BeApproximately(0.6, 0.01);
    }

    [Fact]
    public async Task GetTopicDistributionAsync_OrdersWeakestTopics()
    {
        _context.UserKnowledgeStates.AddRange(
            new UserKnowledgeState
            {
                UserId = _userId,
                Topic = "Chemistry / Bonds",
                MasteryScore = 0.2,
                ConfidenceScore = 0.3,
                ForgettingRisk = 0.9,
                UpdatedAt = DateTime.UtcNow
            },
            new UserKnowledgeState
            {
                UserId = _userId,
                Topic = "History / WW2",
                MasteryScore = 0.7,
                ConfidenceScore = 0.6,
                ForgettingRisk = 0.3,
                UpdatedAt = DateTime.UtcNow
            }
        );

        _context.LearningInteractions.Add(new LearningInteraction
        {
            UserId = _userId,
            Topic = "Chemistry / Bonds",
            Type = LearningInteractionType.QuizAnswer,
            IsCorrect = false,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        var topics = await _service.GetTopicDistributionAsync(10, 1);

        topics.WeakestTopics.Should().HaveCount(1);
        topics.WeakestTopics[0].Topic.Should().Be("Chemistry / Bonds");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
