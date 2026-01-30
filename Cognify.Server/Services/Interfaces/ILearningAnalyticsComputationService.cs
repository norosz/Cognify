using Cognify.Server.Dtos.Analytics;

namespace Cognify.Server.Services.Interfaces;

public interface ILearningAnalyticsComputationService
{
    Task<LearningAnalyticsSummaryDto> GetSummaryAsync(Guid userId, bool includeExams);
    Task<PerformanceTrendsDto> GetTrendsAsync(Guid userId, DateTime? from, DateTime? to, int bucketDays, bool includeExams);
    Task<TopicDistributionDto> GetTopicDistributionAsync(Guid userId, int maxTopics, int maxWeakTopics, bool includeExams);
    Task<List<RetentionHeatmapPointDto>> GetRetentionHeatmapAsync(Guid userId, int maxTopics, bool includeExams);
    Task<DecayForecastDto> GetDecayForecastAsync(Guid userId, int maxTopics, int days, int stepDays, bool includeExams);
    Task<List<MistakePatternSummaryDto>> GetMistakePatternsAsync(Guid userId, int maxItems, int maxTopics, bool includeExams);
}
