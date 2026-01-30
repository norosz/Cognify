using Cognify.Server.Dtos.Analytics;
using Cognify.Server.Services.Interfaces;

namespace Cognify.Server.Services;

public class LearningAnalyticsService(IUserContextService userContext, ILearningAnalyticsComputationService computationService) : ILearningAnalyticsService
{
    public async Task<LearningAnalyticsSummaryDto> GetSummaryAsync(bool includeExams)
    {
        var userId = userContext.GetCurrentUserId();
        return await computationService.GetSummaryAsync(userId, includeExams);
    }

    public async Task<PerformanceTrendsDto> GetTrendsAsync(DateTime? from, DateTime? to, int bucketDays, bool includeExams)
    {
        var userId = userContext.GetCurrentUserId();
        return await computationService.GetTrendsAsync(userId, from, to, bucketDays, includeExams);
    }

    public async Task<TopicDistributionDto> GetTopicDistributionAsync(int maxTopics, int maxWeakTopics, bool includeExams)
    {
        var userId = userContext.GetCurrentUserId();
        return await computationService.GetTopicDistributionAsync(userId, maxTopics, maxWeakTopics, includeExams);
    }

    public async Task<List<RetentionHeatmapPointDto>> GetRetentionHeatmapAsync(int maxTopics, bool includeExams)
    {
        var userId = userContext.GetCurrentUserId();
        return await computationService.GetRetentionHeatmapAsync(userId, maxTopics, includeExams);
    }

    public async Task<DecayForecastDto> GetDecayForecastAsync(int maxTopics, int days, int stepDays, bool includeExams)
    {
        var userId = userContext.GetCurrentUserId();
        return await computationService.GetDecayForecastAsync(userId, maxTopics, days, stepDays, includeExams);
    }

    public async Task<List<MistakePatternSummaryDto>> GetMistakePatternsAsync(int maxItems, int maxTopics, bool includeExams)
    {
        var userId = userContext.GetCurrentUserId();
        return await computationService.GetMistakePatternsAsync(userId, maxItems, maxTopics, includeExams);
    }

    public async Task<CategoryBreakdownDto> GetCategoryBreakdownAsync(bool includeExams, string groupBy, IReadOnlyList<string> quizCategoryFilters, Guid? moduleId = null)
    {
        var userId = userContext.GetCurrentUserId();
        return await computationService.GetCategoryBreakdownAsync(userId, includeExams, groupBy, quizCategoryFilters, moduleId);
    }

    public async Task<List<string>> GetQuizCategoriesAsync()
    {
        var userId = userContext.GetCurrentUserId();
        return await computationService.GetQuizCategoriesAsync(userId);
    }

    public async Task<ExamAnalyticsSummaryDto> GetExamSummaryAsync()
    {
        var userId = userContext.GetCurrentUserId();
        return await computationService.GetExamSummaryAsync(userId);
    }

    public async Task<CategoryBreakdownDto> GetExamCategoryBreakdownAsync()
    {
        var userId = userContext.GetCurrentUserId();
        return await computationService.GetExamCategoryBreakdownAsync(userId);
    }
}
