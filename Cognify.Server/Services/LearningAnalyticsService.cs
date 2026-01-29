using Cognify.Server.Data;
using Cognify.Server.Dtos.Analytics;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class LearningAnalyticsService(ApplicationDbContext db, IUserContextService userContext) : ILearningAnalyticsService
{
    public async Task<LearningAnalyticsSummaryDto> GetSummaryAsync(int days = 30, int trendDays = 14, int maxTopics = 10)
    {
        var userId = userContext.GetCurrentUserId();
        var now = DateTime.UtcNow;
        var from = now.AddDays(-Math.Max(days, 1));
        var trendFrom = now.AddDays(-Math.Max(trendDays, 1));

        var attempts = await db.Attempts
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.CreatedAt >= from)
            .ToListAsync();

        var interactions = await db.LearningInteractions
            .AsNoTracking()
            .Where(i => i.UserId == userId && i.CreatedAt >= from)
            .ToListAsync();

        var states = await db.UserKnowledgeStates
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .ToListAsync();

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
            .Take(Math.Max(maxTopics, 1))
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

    private static List<LearningAnalyticsTrendPointDto> BuildTrends(List<Models.Attempt> attempts, DateTime from, DateTime to)
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
