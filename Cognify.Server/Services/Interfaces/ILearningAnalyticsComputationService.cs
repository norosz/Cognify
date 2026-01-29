using Cognify.Server.Dtos.Analytics;

namespace Cognify.Server.Services.Interfaces;

public interface ILearningAnalyticsComputationService
{
    Task<LearningAnalyticsSummaryDto> GetSummaryAsync(Guid userId);
    Task<PerformanceTrendsDto> GetTrendsAsync(Guid userId, DateTime? from, DateTime? to, int bucketDays);
    Task<TopicDistributionDto> GetTopicDistributionAsync(Guid userId, int maxTopics, int maxWeakTopics);
    Task<List<RetentionHeatmapPointDto>> GetRetentionHeatmapAsync(Guid userId, int maxTopics);
    Task<DecayForecastDto> GetDecayForecastAsync(Guid userId, int maxTopics, int days, int stepDays);
    Task<List<MistakePatternSummaryDto>> GetMistakePatternsAsync(Guid userId, int maxItems, int maxTopics);
}
