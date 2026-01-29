using Cognify.Server.Dtos.Analytics;
using Cognify.Server.Services.Interfaces;

namespace Cognify.Server.Services;

public class LearningAnalyticsService(IUserContextService userContext, ILearningAnalyticsComputationService computationService) : ILearningAnalyticsService
{
    public async Task<LearningAnalyticsSummaryDto> GetSummaryAsync()
    {
        var userId = userContext.GetCurrentUserId();
        return await computationService.GetSummaryAsync(userId);
    }

    public async Task<PerformanceTrendsDto> GetTrendsAsync(DateTime? from, DateTime? to, int bucketDays)
    {
        var userId = userContext.GetCurrentUserId();
        return await computationService.GetTrendsAsync(userId, from, to, bucketDays);
    }

    public async Task<TopicDistributionDto> GetTopicDistributionAsync(int maxTopics, int maxWeakTopics)
    {
        var userId = userContext.GetCurrentUserId();
        return await computationService.GetTopicDistributionAsync(userId, maxTopics, maxWeakTopics);
    }

    public async Task<List<RetentionHeatmapPointDto>> GetRetentionHeatmapAsync(int maxTopics)
    {
        var userId = userContext.GetCurrentUserId();
        return await computationService.GetRetentionHeatmapAsync(userId, maxTopics);
    }

    public async Task<DecayForecastDto> GetDecayForecastAsync(int maxTopics, int days, int stepDays)
    {
        var userId = userContext.GetCurrentUserId();
        return await computationService.GetDecayForecastAsync(userId, maxTopics, days, stepDays);
    }

    public async Task<List<MistakePatternSummaryDto>> GetMistakePatternsAsync(int maxItems, int maxTopics)
    {
        var userId = userContext.GetCurrentUserId();
        return await computationService.GetMistakePatternsAsync(userId, maxItems, maxTopics);
    }
}
