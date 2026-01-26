namespace Cognify.Server.DTOs;

public record AttemptDto
{
    public Guid Id { get; init; }
    public Guid QuestionSetId { get; init; }
    public Guid UserId { get; init; }
    public double Score { get; init; }
    public Dictionary<string, string> Answers { get; init; } = []; // QuestionId (as string) -> Answer
    public DateTime CreatedAt { get; init; }
}

public record SubmitAttemptDto
{
    public Guid QuestionSetId { get; init; }
    public Dictionary<string, string> Answers { get; init; } = []; // QuestionId (as string) -> Answer
}
