using Cognify.Server.Dtos.Analytics;

namespace Cognify.Server.Services.Interfaces;

public interface ILearningAnalyticsService
{
    Task<LearningAnalyticsSummaryDto> GetSummaryAsync(int days = 30, int trendDays = 14, int maxTopics = 10);
}
