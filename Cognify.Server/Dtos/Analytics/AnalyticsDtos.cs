namespace Cognify.Server.Dtos.Analytics;

public record LearningAnalyticsSummaryDto
{
    public int TotalTopics { get; init; }
    public double AverageMastery { get; init; }
    public double AverageForgettingRisk { get; init; }
    public int WeakTopicsCount { get; init; }
    public int TotalAttempts { get; init; }
    public double AccuracyRate { get; init; }
    public double ExamReadinessScore { get; init; }
    public double LearningVelocity { get; init; }
    public DateTime? LastActivityAt { get; init; }
}

public record PerformanceTrendPointDto
{
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public int AttemptCount { get; init; }
    public double AverageScore { get; init; }
}

public record PerformanceTrendsDto
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public int BucketDays { get; init; }
    public List<PerformanceTrendPointDto> Points { get; init; } = [];
}

public record TopicInsightDto
{
    public string Topic { get; init; } = string.Empty;
    public Guid? SourceNoteId { get; init; }
    public double MasteryScore { get; init; }
    public double ForgettingRisk { get; init; }
    public DateTime? LastReviewedAt { get; init; }
    public DateTime? NextReviewAt { get; init; }
    public int AttemptCount { get; init; }
    public double AccuracyRate { get; init; }
    public double WeaknessScore { get; init; }
}

public record TopicDistributionDto
{
    public List<TopicInsightDto> Topics { get; init; } = [];
    public List<TopicInsightDto> WeakestTopics { get; init; } = [];
}

public record RetentionHeatmapPointDto
{
    public string Topic { get; init; } = string.Empty;
    public double MasteryScore { get; init; }
    public double ForgettingRisk { get; init; }
}

public record DecayForecastPointDto
{
    public DateTime Date { get; init; }
    public double ForgettingRisk { get; init; }
}

public record DecayForecastTopicDto
{
    public string Topic { get; init; } = string.Empty;
    public List<DecayForecastPointDto> Points { get; init; } = [];
}

public record DecayForecastDto
{
    public int Days { get; init; }
    public int StepDays { get; init; }
    public List<DecayForecastTopicDto> Topics { get; init; } = [];
}

public record MistakePatternTopicDto
{
    public string Topic { get; init; } = string.Empty;
    public int Count { get; init; }
}

public record MistakePatternSummaryDto
{
    public string Category { get; init; } = string.Empty;
    public int TotalCount { get; init; }
    public List<MistakePatternTopicDto> TopTopics { get; init; } = [];
}

public record CategoryBreakdownItemDto
{
    public string CategoryLabel { get; init; } = "Uncategorized";
    public int ModuleCount { get; init; }
    public int QuizCount { get; init; }
    public int PracticeAttemptCount { get; init; }
    public double PracticeAverageScore { get; init; }
    public double PracticeBestScore { get; init; }
    public DateTime? LastPracticeAttemptAt { get; init; }
    public int ExamAttemptCount { get; init; }
    public double ExamAverageScore { get; init; }
    public double ExamBestScore { get; init; }
    public DateTime? LastExamAttemptAt { get; init; }
}

public record CategoryBreakdownDto
{
    public List<CategoryBreakdownItemDto> Items { get; init; } = [];
}
