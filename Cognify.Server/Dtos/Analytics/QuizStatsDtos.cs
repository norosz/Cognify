namespace Cognify.Server.Dtos.Analytics;

public record QuizStatsDto
{
    public Guid QuizId { get; init; }
    public int QuestionCount { get; init; }
    public int AttemptCount { get; init; }
    public double AverageScore { get; init; }
    public double BestScore { get; init; }
    public DateTime? LastAttemptAt { get; init; }
    public double AccuracyRate { get; init; }
}
