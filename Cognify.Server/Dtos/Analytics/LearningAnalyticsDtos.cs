namespace Cognify.Server.Dtos.Analytics;

public record LearningAnalyticsSummaryDto(
    DateTime From,
    DateTime To,
    int TotalAttempts,
    double AverageScore,
    int TotalInteractions,
    double CorrectRate,
    int ActiveTopics,
    double AverageMastery,
    double AverageForgettingRisk,
    IReadOnlyList<LearningAnalyticsTrendPointDto> Trends,
    IReadOnlyList<TopicDistributionDto> Topics
);

public record LearningAnalyticsTrendPointDto(
    DateTime Date,
    int Attempts,
    double AverageScore
);

public record TopicDistributionDto(
    string Topic,
    double MasteryScore,
    double ForgettingRisk,
    DateTime? LastReviewedAt,
    DateTime? NextReviewAt
);
