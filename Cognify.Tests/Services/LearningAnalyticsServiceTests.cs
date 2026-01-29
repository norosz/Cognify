using Cognify.Server.Data;
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
    private readonly Guid _userId;

    public LearningAnalyticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _userContextMock = new Mock<IUserContextService>();
        _userId = Guid.NewGuid();
        _userContextMock.Setup(x => x.GetCurrentUserId()).Returns(_userId);
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsAggregatedMetrics()
    {
        var now = DateTime.UtcNow;

        _context.Attempts.AddRange(
            new Attempt { UserId = _userId, QuestionSetId = Guid.NewGuid(), AnswersJson = "{}", Score = 80, CreatedAt = now.AddDays(-1) },
            new Attempt { UserId = _userId, QuestionSetId = Guid.NewGuid(), AnswersJson = "{}", Score = 60, CreatedAt = now.AddDays(-1) }
        );

        _context.LearningInteractions.AddRange(
            new LearningInteraction { UserId = _userId, Topic = "Topic A", IsCorrect = true, CreatedAt = now.AddDays(-1) },
            new LearningInteraction { UserId = _userId, Topic = "Topic A", IsCorrect = false, CreatedAt = now.AddDays(-1) }
        );

        _context.UserKnowledgeStates.AddRange(
            new UserKnowledgeState { UserId = _userId, Topic = "Topic A", MasteryScore = 0.5, ConfidenceScore = 0.5, ForgettingRisk = 0.4, UpdatedAt = now },
            new UserKnowledgeState { UserId = _userId, Topic = "Topic B", MasteryScore = 0.7, ConfidenceScore = 0.7, ForgettingRisk = 0.2, UpdatedAt = now }
        );

        await _context.SaveChangesAsync();

        var service = new LearningAnalyticsService(_context, _userContextMock.Object);
        var summary = await service.GetSummaryAsync(days: 7, trendDays: 1, maxTopics: 5);

        summary.TotalAttempts.Should().Be(2);
        summary.AverageScore.Should().BeApproximately(70, 0.1);
        summary.TotalInteractions.Should().Be(2);
        summary.CorrectRate.Should().BeApproximately(0.5, 0.01);
        summary.ActiveTopics.Should().Be(2);
        summary.Trends.Should().HaveCount(2);
        summary.Topics.Should().HaveCount(2);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
