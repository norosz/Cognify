namespace Cognify.Server.Dtos.Analytics;

public record ModuleWeakTopicDto
{
    public required string Topic { get; init; }
    public double MasteryScore { get; init; }
    public double ForgettingRisk { get; init; }
    public DateTime? NextReviewAt { get; init; }
}

public record ModuleStatsDto
{
    public Guid ModuleId { get; init; }
    public int TotalDocuments { get; init; }
    public int TotalNotes { get; init; }
    public int TotalQuizzes { get; init; }

    public double AverageMastery { get; init; }
    public double AverageForgettingRisk { get; init; }

    public int PracticeAttemptCount { get; init; }
    public double PracticeAverageScore { get; init; }
    public double PracticeBestScore { get; init; }
    public DateTime? LastPracticeAttemptAt { get; init; }

    public int ExamAttemptCount { get; init; }
    public double ExamAverageScore { get; init; }
    public double ExamBestScore { get; init; }
    public DateTime? LastExamAttemptAt { get; init; }

    public List<ModuleWeakTopicDto> WeakTopics { get; init; } = [];
}
