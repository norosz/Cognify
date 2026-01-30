namespace Cognify.Server.Dtos.Attempt;

public record AttemptReviewDto
{
    public Guid AttemptId { get; init; }
    public Guid QuizId { get; init; }
    public double Score { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<AttemptReviewItemDto> Items { get; init; } = [];
}

public record AttemptReviewItemDto
{
    public Guid QuestionId { get; init; }
    public string QuestionType { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public string? UserAnswer { get; init; }
    public string? CorrectAnswer { get; init; }
    public bool? IsCorrect { get; init; }
    public string? Explanation { get; init; }
    public string? Feedback { get; init; }
    public List<string> DetectedMistakes { get; init; } = [];
    public double? ConfidenceEstimate { get; init; }
}
