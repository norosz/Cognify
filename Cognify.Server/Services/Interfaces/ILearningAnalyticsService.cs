using Cognify.Server.Dtos.Analytics;

namespace Cognify.Server.Services.Interfaces;

public interface ILearningAnalyticsService
{
    Task<LearningAnalyticsSummaryDto> GetSummaryAsync();
    Task<PerformanceTrendsDto> GetTrendsAsync(DateTime? from, DateTime? to, int bucketDays);
    Task<TopicDistributionDto> GetTopicDistributionAsync(int maxTopics, int maxWeakTopics);
    Task<List<RetentionHeatmapPointDto>> GetRetentionHeatmapAsync(int maxTopics);
    Task<DecayForecastDto> GetDecayForecastAsync(int maxTopics, int days, int stepDays);
    Task<List<MistakePatternSummaryDto>> GetMistakePatternsAsync(int maxItems, int maxTopics);
}
