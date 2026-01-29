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
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(6);

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
                logger.LogError(ex, "Learning analytics background worker loop failed.");
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
        var agentRunService = scope.ServiceProvider.GetRequiredService<IAgentRunService>();

        var today = DateTime.UtcNow.Date;

        var activeUserIds = await db.UserKnowledgeStates
            .AsNoTracking()
            .Select(s => s.UserId)
            .Distinct()
            .ToListAsync(stoppingToken);

        foreach (var userId in activeUserIds)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var alreadyRun = await db.AgentRuns
                .AsNoTracking()
                .AnyAsync(r => r.UserId == userId && r.Type == AgentRunType.Analytics && r.CreatedAt >= today, stoppingToken);

            if (alreadyRun)
            {
                continue;
            }

            var inputHash = AgentRunService.ComputeHash($"analytics:{userId}:{today:yyyyMMdd}");
            var run = await agentRunService.CreateAsync(userId, AgentRunType.Analytics, inputHash, promptVersion: "analytics-v1");

            try
            {
                var summary = await BuildSummaryAsync(db, userId, stoppingToken);
                var output = JsonSerializer.Serialize(summary);
                await agentRunService.MarkCompletedAsync(run.Id, output);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Learning analytics computation failed for {UserId}", userId);
                await agentRunService.MarkFailedAsync(run.Id, "Analytics computation failed.");
            }
        }
    }

    private static async Task<LearningAnalyticsSummaryDto> BuildSummaryAsync(ApplicationDbContext db, Guid userId, CancellationToken stoppingToken)
    {
        var now = DateTime.UtcNow;
        var from = now.AddDays(-30);
        var trendFrom = now.AddDays(-14);

        var attempts = await db.Attempts
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.CreatedAt >= from)
            .ToListAsync(stoppingToken);

        var interactions = await db.LearningInteractions
            .AsNoTracking()
            .Where(i => i.UserId == userId && i.CreatedAt >= from)
            .ToListAsync(stoppingToken);

        var states = await db.UserKnowledgeStates
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .ToListAsync(stoppingToken);

        var totalAttempts = attempts.Count;
        var averageScore = totalAttempts > 0 ? attempts.Average(a => a.Score) : 0;

        var totalInteractions = interactions.Count;
        var correctRate = totalInteractions > 0
            ? interactions.Count(i => i.IsCorrect == true) / (double)totalInteractions
            : 0;

        var activeTopics = states.Count;
        var averageMastery = activeTopics > 0 ? states.Average(s => s.MasteryScore) : 0;
        var averageForgetting = activeTopics > 0 ? states.Average(s => s.ForgettingRisk) : 0;

        var trends = BuildTrends(attempts, trendFrom, now);
        var topics = states
            .OrderByDescending(s => s.ForgettingRisk)
            .ThenBy(s => s.MasteryScore)
            .Take(10)
            .Select(s => new TopicDistributionDto(
                s.Topic,
                s.MasteryScore,
                s.ForgettingRisk,
                s.LastReviewedAt,
                s.NextReviewAt))
            .ToList();

        return new LearningAnalyticsSummaryDto(
            from,
            now,
            totalAttempts,
            averageScore,
            totalInteractions,
            correctRate,
            activeTopics,
            averageMastery,
            averageForgetting,
            trends,
            topics);
    }

    private static List<LearningAnalyticsTrendPointDto> BuildTrends(List<Attempt> attempts, DateTime from, DateTime to)
    {
        var grouped = attempts
            .Where(a => a.CreatedAt >= from)
            .GroupBy(a => a.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => new
            {
                Attempts = g.Count(),
                AverageScore = g.Average(a => a.Score)
            });

        var points = new List<LearningAnalyticsTrendPointDto>();
        for (var day = from.Date; day <= to.Date; day = day.AddDays(1))
        {
            if (grouped.TryGetValue(day, out var data))
            {
                points.Add(new LearningAnalyticsTrendPointDto(day, data.Attempts, data.AverageScore));
            }
            else
            {
                points.Add(new LearningAnalyticsTrendPointDto(day, 0, 0));
            }
        }

        return points;
    }
}
