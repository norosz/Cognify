using System.Reflection;
using Cognify.Server.Data;
using Cognify.Server.Models;
using Cognify.Server.Services;
using Cognify.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Cognify.Tests.Services;

public class LearningAnalyticsBackgroundWorkerTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public LearningAnalyticsBackgroundWorkerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task BuildSummaryAsync_ReturnsSummary()
    {
        var userId = Guid.NewGuid();
        _context.Attempts.Add(new Attempt { UserId = userId, QuestionSetId = Guid.NewGuid(), AnswersJson = "{}", Score = 90, CreatedAt = DateTime.UtcNow });
        _context.LearningInteractions.Add(new LearningInteraction { UserId = userId, Topic = "Topic", IsCorrect = true, CreatedAt = DateTime.UtcNow });
        _context.UserKnowledgeStates.Add(new UserKnowledgeState { UserId = userId, Topic = "Topic", MasteryScore = 0.7, ConfidenceScore = 0.7, ForgettingRisk = 0.3, UpdatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var method = typeof(LearningAnalyticsBackgroundWorker).GetMethod("BuildSummaryAsync", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var task = (Task)method!.Invoke(null, new object[] { _context, userId, CancellationToken.None })!;
        await task.ConfigureAwait(false);

        var resultProperty = task.GetType().GetProperty("Result");
        var summary = resultProperty!.GetValue(task) as Cognify.Server.Dtos.Analytics.LearningAnalyticsSummaryDto;

        summary.Should().NotBeNull();
        summary!.TotalAttempts.Should().Be(1);
        summary.ActiveTopics.Should().Be(1);
    }

    [Fact]
    public async Task ProcessAnalyticsAsync_CreatesAgentRun()
    {
        var userId = Guid.NewGuid();
        _context.UserKnowledgeStates.Add(new UserKnowledgeState { UserId = userId, Topic = "Topic", MasteryScore = 0.5, ConfidenceScore = 0.5, ForgettingRisk = 0.5, UpdatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var agentRunService = new Mock<IAgentRunService>();
        agentRunService.Setup(s => s.CreateAsync(It.IsAny<Guid>(), AgentRunType.Analytics, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new AgentRun { Id = Guid.NewGuid(), UserId = userId, Type = AgentRunType.Analytics });

        var services = new ServiceCollection();
        services.AddSingleton(_context);
        services.AddScoped<IAgentRunService>(_ => agentRunService.Object);
        services.AddLogging();

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var worker = new LearningAnalyticsBackgroundWorker(scopeFactory, NullLogger<LearningAnalyticsBackgroundWorker>.Instance);

        var method = typeof(LearningAnalyticsBackgroundWorker).GetMethod("ProcessAnalyticsAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        method.Should().NotBeNull();

        var task = (Task)method!.Invoke(worker, new object[] { CancellationToken.None })!;
        await task.ConfigureAwait(false);

        agentRunService.Verify(s => s.CreateAsync(userId, AgentRunType.Analytics, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
