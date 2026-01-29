namespace Cognify.Server.Dtos.Knowledge;

public record UserKnowledgeStateDto
{
    public Guid Id { get; init; }
    public string Topic { get; init; } = string.Empty;
    public Guid? SourceNoteId { get; init; }
    public double MasteryScore { get; init; }
    public double ConfidenceScore { get; init; }
    public double ForgettingRisk { get; init; }
    public DateTime? NextReviewAt { get; init; }
    public DateTime? LastReviewedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record ReviewQueueItemDto
{
    public string Topic { get; init; } = string.Empty;
    public Guid? SourceNoteId { get; init; }
    public double MasteryScore { get; init; }
    public double ForgettingRisk { get; init; }
    public DateTime? NextReviewAt { get; init; }
}
