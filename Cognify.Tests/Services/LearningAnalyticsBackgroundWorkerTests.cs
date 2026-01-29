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
    public async Task ProcessAnalyticsAsync_CreatesAgentRun()
    {
        var userId = Guid.NewGuid();
        _context.UserKnowledgeStates.Add(new UserKnowledgeState { UserId = userId, Topic = "Topic", MasteryScore = 0.5, ConfidenceScore = 0.5, ForgettingRisk = 0.5, UpdatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var agentRunService = new Mock<IAgentRunService>();
        agentRunService.Setup(s => s.CreateAsync(It.IsAny<Guid>(), AgentRunType.Analytics, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new AgentRun { Id = Guid.NewGuid(), UserId = userId, Type = AgentRunType.Analytics });

        agentRunService.Setup(s => s.MarkRunningAsync(It.IsAny<Guid>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
        agentRunService.Setup(s => s.MarkCompletedAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .Returns(Task.CompletedTask);

        var analytics = new Mock<ILearningAnalyticsComputationService>();
        analytics.Setup(s => s.GetSummaryAsync(userId)).ReturnsAsync(new Cognify.Server.Dtos.Analytics.LearningAnalyticsSummaryDto());
        analytics.Setup(s => s.GetTrendsAsync(userId, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>()))
            .ReturnsAsync(new Cognify.Server.Dtos.Analytics.PerformanceTrendsDto());
        analytics.Setup(s => s.GetTopicDistributionAsync(userId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new Cognify.Server.Dtos.Analytics.TopicDistributionDto());
        analytics.Setup(s => s.GetRetentionHeatmapAsync(userId, It.IsAny<int>()))
            .ReturnsAsync([]);
        analytics.Setup(s => s.GetDecayForecastAsync(userId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new Cognify.Server.Dtos.Analytics.DecayForecastDto());

        var services = new ServiceCollection();
        services.AddScoped<ApplicationDbContext>(_ => _context);
        services.AddScoped<ILearningAnalyticsComputationService>(_ => analytics.Object);
        services.AddScoped<IAgentRunService>(_ => agentRunService.Object);
        services.AddLogging();

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var worker = new LearningAnalyticsBackgroundWorker(scopeFactory, NullLogger<LearningAnalyticsBackgroundWorker>.Instance);

        var method = typeof(LearningAnalyticsBackgroundWorker).GetMethod("ProcessAnalyticsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Should().NotBeNull();

        var task = (Task)method!.Invoke(worker, new object[] { CancellationToken.None })!;
        await task.ConfigureAwait(false);

        agentRunService.Verify(s => s.CreateAsync(userId, AgentRunType.Analytics, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
        agentRunService.Verify(s => s.MarkCompletedAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
    }

    public void Dispose()
    {
        // InMemory provider + per-test database name means cleanup isn't needed,
        // and the context may already be disposed by the DI scope.
        _context.Dispose();
    }
}
