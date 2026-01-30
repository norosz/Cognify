using Cognify.Server.Dtos.Analytics;

namespace Cognify.Server.Services.Interfaces;

public interface ILearningAnalyticsService
{
    Task<LearningAnalyticsSummaryDto> GetSummaryAsync(bool includeExams);
    Task<PerformanceTrendsDto> GetTrendsAsync(DateTime? from, DateTime? to, int bucketDays, bool includeExams);
    Task<TopicDistributionDto> GetTopicDistributionAsync(int maxTopics, int maxWeakTopics, bool includeExams);
    Task<List<RetentionHeatmapPointDto>> GetRetentionHeatmapAsync(int maxTopics, bool includeExams);
    Task<DecayForecastDto> GetDecayForecastAsync(int maxTopics, int days, int stepDays, bool includeExams);
    Task<List<MistakePatternSummaryDto>> GetMistakePatternsAsync(int maxItems, int maxTopics, bool includeExams);
}
