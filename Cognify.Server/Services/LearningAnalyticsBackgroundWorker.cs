using System.Text.Json;
using Cognify.Server.Data;
using Cognify.Server.Dtos.Analytics;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class LearningAnalyticsBackgroundWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<LearningAnalyticsBackgroundWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(15);
    private const int LookbackDays = 30;
    private const int BatchSize = 25;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAnalyticsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Learning analytics background worker failed.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessAnalyticsAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var analytics = scope.ServiceProvider.GetRequiredService<ILearningAnalyticsComputationService>();
        var agentRunService = scope.ServiceProvider.GetRequiredService<IAgentRunService>();

        var now = DateTime.UtcNow;
        var since = now.AddDays(-LookbackDays);

        var activeUserIds = await db.UserKnowledgeStates
            .AsNoTracking()
            .Where(s => s.UpdatedAt >= since)
            .Select(s => s.UserId)
            .Union(db.Attempts.AsNoTracking().Where(a => a.CreatedAt >= since).Select(a => a.UserId))
            .Union(db.LearningInteractions.AsNoTracking().Where(i => i.CreatedAt >= since).Select(i => i.UserId))
            .Distinct()
            .Take(BatchSize)
            .ToListAsync(stoppingToken);

        foreach (var userId in activeUserIds)
        {
            try
            {
                var alreadyRan = await db.AgentRuns
                    .AsNoTracking()
                    .AnyAsync(r => r.UserId == userId
                                   && r.Type == AgentRunType.Analytics
                                   && r.CreatedAt >= now.Date,
                        stoppingToken);

                if (alreadyRan)
                {
                    continue;
                }

                var inputHash = AgentRunService.ComputeHash($"analytics:{userId}:{now:yyyyMMdd}");
                var run = await agentRunService.CreateAsync(userId, AgentRunType.Analytics, inputHash, promptVersion: "analytics-v1");
                await agentRunService.MarkRunningAsync(run.Id, model: "statistical-engine");

                var summary = await analytics.GetSummaryAsync(userId);
                var trends = await analytics.GetTrendsAsync(userId, null, null, 7);
                var topics = await analytics.GetTopicDistributionAsync(userId, 20, 5);
                var heatmap = await analytics.GetRetentionHeatmapAsync(userId, 30);
                var forecast = await analytics.GetDecayForecastAsync(userId, 6, 14, 2);

                var payload = new
                {
                    GeneratedAt = now,
                    Summary = summary,
                    Trends = trends,
                    Topics = topics,
                    Heatmap = heatmap,
                    Forecast = forecast
                };

                var outputJson = JsonSerializer.Serialize(payload);
                await agentRunService.MarkCompletedAsync(run.Id, outputJson);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Learning analytics agent failed for user {UserId}", userId);
                var failedRun = await db.AgentRuns
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.Type == AgentRunType.Analytics && r.Status == AgentRunStatus.Running, stoppingToken);

                if (failedRun != null)
                {
                    await agentRunService.MarkFailedAsync(failedRun.Id, "Learning analytics agent failed.");
                }
            }
        }
    }
}
