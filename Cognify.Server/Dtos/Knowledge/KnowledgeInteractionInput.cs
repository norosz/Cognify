namespace Cognify.Server.Dtos.Knowledge;

public record KnowledgeInteractionInput
{
    public Guid QuestionId { get; init; }
    public string? UserAnswer { get; init; }
    public bool IsCorrect { get; init; }
    public double Score { get; init; }
    public double MaxScore { get; init; } = 1;
    public string? Feedback { get; init; }
    public IReadOnlyList<string>? DetectedMistakes { get; init; }
}
