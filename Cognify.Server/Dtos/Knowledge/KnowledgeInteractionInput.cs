namespace Cognify.Server.Dtos.Knowledge;

public record KnowledgeInteractionInput
{
    public Guid QuestionId { get; init; }
    public string? UserAnswer { get; init; }
    public bool IsCorrect { get; init; }
}
